#!/usr/bin/env python3

"""
Attendance Sync System
Automated synchronization between ZK Biometric Device and managerCMN system.
"""

import json
import logging
import requests
import pytz
from datetime import datetime, timedelta
from zk import ZK
from typing import List, Dict, Optional
from pathlib import Path
import time
import sys
import os
import fcntl  # For file locking on Unix/Linux


class SingleInstanceLock:
    """Ensures only one instance of the script runs at a time using file locking"""
    def __init__(self, lock_file: str = "/tmp/attendance_sync.lock"):
        self.lock_file = lock_file
        self.lock_fd = None

    def acquire(self) -> bool:
        """Try to acquire the lock. Returns True if successful, False if another instance is running."""
        try:
            self.lock_fd = open(self.lock_file, 'w')
            fcntl.flock(self.lock_fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
            # Write PID to lock file for debugging
            self.lock_fd.write(str(os.getpid()))
            self.lock_fd.flush()
            return True
        except (IOError, OSError):
            if self.lock_fd:
                self.lock_fd.close()
            return False

    def release(self):
        """Release the lock"""
        if self.lock_fd:
            try:
                fcntl.flock(self.lock_fd, fcntl.LOCK_UN)
                self.lock_fd.close()
            except:
                pass


class AttendanceSync:
    def __init__(self, config_path: str = "config.json"):
        """Initialize the attendance sync system"""
        self.config_path = config_path
        self.config = self.load_config(config_path)
        self.setup_logging()
        self.vietnam_tz = pytz.timezone('Asia/Ho_Chi_Minh')
        self.last_sync_file = 'last_sync.json'

    def load_config(self, config_path: str) -> Dict:
        """Load configuration from JSON file"""
        try:
            with open(config_path, 'r', encoding='utf-8') as f:
                return json.load(f)
        except FileNotFoundError:
            print(f"Error: Configuration file {config_path} not found!")
            sys.exit(1)
        except json.JSONDecodeError as e:
            print(f"Error: Invalid JSON in {config_path}: {e}")
            sys.exit(1)

    def setup_logging(self):
        """Setup logging system with file and console output"""
        log_dir = Path("logs")
        log_dir.mkdir(exist_ok=True)

        # Create formatters
        file_formatter = logging.Formatter(
            '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
        )
        console_formatter = logging.Formatter(
            '%(asctime)s - %(levelname)s - %(message)s'
        )

        # Setup file handler with rotation
        file_handler = logging.FileHandler(log_dir / 'attendance_sync.log')
        file_handler.setFormatter(file_formatter)
        file_handler.setLevel(logging.INFO)

        # Setup console handler
        console_handler = logging.StreamHandler()
        console_handler.setFormatter(console_formatter)
        console_handler.setLevel(logging.INFO)

        # Configure logger
        self.logger = logging.getLogger('attendance_sync')
        self.logger.setLevel(logging.INFO)
        self.logger.addHandler(file_handler)
        self.logger.addHandler(console_handler)

    def get_last_sync_time(self) -> Optional[datetime]:
        """Get the last successful sync timestamp"""
        try:
            with open(self.last_sync_file, 'r') as f:
                data = json.load(f)
                # Parse ISO format datetime and convert to Vietnam timezone
                # Python 3.6 compatible datetime parsing
                timestamp_str = data['last_sync']
                if timestamp_str.endswith('Z'):
                    timestamp_str = timestamp_str[:-1] + '+00:00'

                # Handle different ISO format variations for Python 3.6
                if '+' in timestamp_str[-6:] or '-' in timestamp_str[-6:]:
                    # Has timezone info like: 2026-03-24T10:30:00+07:00
                    dt_part, tz_part = timestamp_str.rsplit('+' if '+' in timestamp_str[-6:] else '-', 1)
                    tz_sign = '+' if '+' in timestamp_str[-6:] else '-'

                    # Parse main datetime part
                    if '.' in dt_part:
                        # Has microseconds
                        utc_time = datetime.strptime(dt_part, '%Y-%m-%dT%H:%M:%S.%f')
                    else:
                        # No microseconds
                        utc_time = datetime.strptime(dt_part, '%Y-%m-%dT%H:%M:%S')

                    # Parse timezone offset
                    if ':' in tz_part:
                        tz_hours, tz_mins = tz_part.split(':')
                    else:
                        tz_hours, tz_mins = tz_part[:2], tz_part[2:] if len(tz_part) == 4 else '00'

                    # Apply timezone offset to get UTC time
                    offset_delta = timedelta(hours=int(tz_hours), minutes=int(tz_mins))
                    if tz_sign == '+':
                        utc_time = utc_time - offset_delta
                    else:
                        utc_time = utc_time + offset_delta

                    # Set UTC timezone and convert to Vietnam timezone
                    utc_time = utc_time.replace(tzinfo=pytz.UTC)
                else:
                    # No timezone info, assume UTC
                    if '.' in timestamp_str:
                        utc_time = datetime.strptime(timestamp_str, '%Y-%m-%dT%H:%M:%S.%f')
                    else:
                        utc_time = datetime.strptime(timestamp_str, '%Y-%m-%dT%H:%M:%S')
                    utc_time = utc_time.replace(tzinfo=pytz.UTC)

                return utc_time.astimezone(self.vietnam_tz)
        except (FileNotFoundError, KeyError, ValueError) as e:
            self.logger.info(f"No previous sync time found: {e}")
            return None

    def save_last_sync_time(self, sync_time: datetime):
        """Save the last successful sync timestamp"""
        try:
            # Convert to UTC for storage
            utc_time = sync_time.astimezone(pytz.UTC)
            with open(self.last_sync_file, 'w') as f:
                json.dump({
                    'last_sync': utc_time.isoformat(),
                    'local_time': sync_time.isoformat(),
                    'timezone': str(sync_time.tzinfo)
                }, f, indent=2)
            self.logger.info(f"Saved last sync time: {sync_time}")
        except Exception as e:
            self.logger.error(f"Failed to save last sync time: {e}")

    def is_night_hours(self) -> bool:
        """Check if current Vietnam time is between 8 PM and 7 AM"""
        if not self.config.get('sync', {}).get('skip_night_hours', True):
            return False

        vn_time = datetime.now(self.vietnam_tz)
        hour = vn_time.hour
        is_night = hour >= 20 or hour < 7

        if is_night:
            self.logger.info(f"Night hours detected (Vietnam time: {vn_time.strftime('%H:%M')}), skipping sync")

        return is_night

    def connect_to_device(self) -> Optional[object]:
        """Connect to ZK biometric device"""
        try:
            device_config = self.config['device']
            zk = ZK(
                device_config['ip'],
                port=device_config.get('port', 4370),
                force_udp=True,
                timeout=device_config.get('timeout', 10)
            )
            conn = zk.connect()
            self.logger.info(f"Connected to ZK device at {device_config['ip']}:{device_config.get('port', 4370)}")
            return conn
        except Exception as e:
            self.logger.error(f"Failed to connect to ZK device: {e}")
            return None

    def get_new_records(self, conn, last_sync: Optional[datetime]) -> List[Dict]:
        """Fetch new attendance records from the device"""
        try:
            self.logger.info("Fetching attendance logs from device...")
            logs = conn.get_attendance()
            new_records = []

            for log in logs:
                try:
                    # Convert timestamp to Vietnam timezone
                    if log.timestamp.tzinfo is None:
                        # Assume device time is in Vietnam timezone if no timezone info
                        log_time = self.vietnam_tz.localize(log.timestamp)
                    else:
                        log_time = log.timestamp.astimezone(self.vietnam_tz)

                    # Only include records newer than last sync
                    if last_sync is None or log_time > last_sync:
                        new_records.append({
                            "UserId": str(log.user_id),
                            "Time": log_time.isoformat()
                        })

                except Exception as e:
                    self.logger.warning(f"Skipping invalid log entry: {e}")
                    continue

            self.logger.info(f"Found {len(new_records)} new attendance records")
            if new_records:
                # Log first few records for verification
                sample_records = new_records[:3]
                for record in sample_records:
                    self.logger.info(f"Sample record: UserId={record['UserId']}, Time={record['Time']}")

            return new_records

        except Exception as e:
            self.logger.error(f"Failed to fetch attendance records: {e}")
            return []

    def send_to_server(self, records: List[Dict]) -> bool:
        """Send attendance records to the server API"""
        if not records:
            self.logger.info("No records to send to server")
            return True

        api_config = self.config['api']

        try:
            headers = {
                'Content-Type': 'application/json',
                'X-API-Key': api_config['key'],
                'User-Agent': 'AttendanceSync/1.0'
            }

            self.logger.info(f"Sending {len(records)} records to {api_config['endpoint']}")

            response = requests.post(
                api_config['endpoint'],
                json=records,
                headers=headers,
                timeout=api_config.get('timeout', 30)
            )

            if response.status_code == 200:
                response_data = response.json()
                self.logger.info(f"Successfully sent records: {response_data}")
                return True
            else:
                self.logger.error(f"Server error: {response.status_code} - {response.text}")
                return False

        except requests.exceptions.ConnectionError as e:
            self.logger.error(f"Connection error: {e}")
            return False
        except requests.exceptions.Timeout as e:
            self.logger.error(f"Request timeout: {e}")
            return False
        except requests.exceptions.RequestException as e:
            self.logger.error(f"Network error sending records: {e}")
            return False
        except Exception as e:
            self.logger.error(f"Unexpected error sending records: {e}")
            return False

    def send_to_server_with_retry(self, records: List[Dict]) -> bool:
        """Send records to server with retry logic"""
        max_retries = self.config.get('sync', {}).get('max_retries', 3)
        retry_delay = self.config.get('sync', {}).get('retry_delay_seconds', 30)

        for attempt in range(max_retries):
            try:
                if self.send_to_server(records):
                    return True

                if attempt < max_retries - 1:
                    self.logger.warning(f"Attempt {attempt + 1} failed, retrying in {retry_delay} seconds...")
                    time.sleep(retry_delay)

            except Exception as e:
                self.logger.error(f"Attempt {attempt + 1} error: {e}")
                if attempt < max_retries - 1:
                    time.sleep(retry_delay)

        self.logger.error(f"Failed to send records after {max_retries} attempts")
        return False

    def run_sync(self):
        """Main synchronization function"""
        try:
            self.logger.info("=" * 50)
            self.logger.info("Starting attendance synchronization")

            # Check if it's night hours
            if self.is_night_hours():
                return

            # Get last sync time
            last_sync = self.get_last_sync_time()
            current_time = datetime.now(self.vietnam_tz)

            if last_sync:
                self.logger.info(f"Last sync: {last_sync}")
            else:
                self.logger.info("First time sync - will fetch all available records")

            # Connect to device
            conn = self.connect_to_device()
            if not conn:
                self.logger.error("Cannot connect to device, aborting sync")
                return

            try:
                # Get new records
                new_records = self.get_new_records(conn, last_sync)

                # Send to server with retry
                if self.send_to_server_with_retry(new_records):
                    # Update last sync time only on success
                    self.save_last_sync_time(current_time)
                    self.logger.info("Synchronization completed successfully")
                else:
                    self.logger.error("Synchronization failed - server request failed")

            finally:
                # Always disconnect from device
                try:
                    conn.disconnect()
                    self.logger.info("Disconnected from ZK device")
                except Exception as e:
                    self.logger.warning(f"Error disconnecting from device: {e}")

        except Exception as e:
            self.logger.error(f"Unexpected error during sync: {e}")
            import traceback
            self.logger.error(f"Traceback: {traceback.format_exc()}")

def main():
    """Main entry point"""
    # Acquire single instance lock to prevent multiple simultaneous runs
    lock = SingleInstanceLock()
    if not lock.acquire():
        print("Another instance is already running. Exiting.")
        sys.exit(0)

    try:
        # Change to script directory
        script_dir = os.path.dirname(os.path.abspath(__file__))
        os.chdir(script_dir)

        # Initialize and run sync
        sync = AttendanceSync()
        sync.run_sync()

    except KeyboardInterrupt:
        print("Sync interrupted by user")
    except Exception as e:
        print(f"Fatal error: {e}")
        import traceback
        traceback.print_exc()
    finally:
        lock.release()

if __name__ == "__main__":
    main()