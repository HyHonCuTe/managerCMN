/* ── Project Management JS ── */

let currentOpenTaskId = null;
let taskPanelNoticeTimer = null;

document.addEventListener('DOMContentLoaded', function () {
    const panel = document.getElementById('taskDetailPanel');
    const overlay = document.getElementById('taskDetailOverlay');

    document.querySelectorAll('.task-expand-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const taskId = this.dataset.taskId;
            const children = document.querySelectorAll(`.task-subtree[data-parent="${taskId}"]`);
            const isExpanded = this.dataset.expanded === 'true';
            children.forEach(el => {
                el.style.display = isExpanded ? 'none' : '';
                el.querySelectorAll('.task-subtree').forEach(sub => {
                    sub.style.display = 'none';
                    const subBtn = document.querySelector(`.task-expand-btn[data-task-id="${sub.dataset.parent}"]`);
                    if (subBtn) {
                        subBtn.dataset.expanded = 'false';
                        updateExpandIcon(subBtn, false);
                    }
                });
            });
            this.dataset.expanded = isExpanded ? 'false' : 'true';
            updateExpandIcon(this, !isExpanded);
        });
    });

    function updateExpandIcon(btn, expanded) {
        btn.innerHTML = expanded
            ? '<i class="bi bi-caret-down-fill"></i>'
            : '<i class="bi bi-caret-right-fill"></i>';
    }

    window.openTaskPanel = function (taskId) {
        currentOpenTaskId = Number(taskId);
        const content = document.getElementById('taskDetailContent');
        if (!panel || !content) {
            showToast('Không tìm thấy khu vực hiển thị chi tiết task.', 'danger');
            return;
        }

        fetch(`/ProjectTask/Details/${taskId}`)
            .then(async r => {
                const html = await r.text();
                if (!r.ok) {
                    throw new Error(html || `HTTP ${r.status}`);
                }
                return html;
            })
            .then(html => {
                content.innerHTML = html;
                panel.classList.add('open');
                overlay?.classList.add('open');
                bindTaskPanel(content);
                clearOpenTaskQueryParam();
            })
            .catch(() => showToast('Không tải được chi tiết task. Mình đã chặn lỗi im lặng; nếu vẫn còn, mình sẽ truy tiếp route này.', 'danger'));
    };

    window.handleTaskRowClick = function (event, taskId) {
        const interactiveTarget = event.target.closest(
            '.task-expand-btn, a, button, input, select, textarea, label, form'
        );

        if (interactiveTarget) {
            return;
        }

        window.openTaskPanel(taskId);
    };

    window.closeTaskPanel = function () {
        if (panel) panel.classList.remove('open');
        if (overlay) overlay.classList.remove('open');
        currentOpenTaskId = null;
    };

    window.reloadProjectPageForTask = function (taskId = currentOpenTaskId) {
        const url = new URL(window.location.href);
        if (taskId) {
            url.searchParams.set('openTaskId', taskId);
        } else {
            url.searchParams.delete('openTaskId');
        }

        window.location.assign(url.toString());
    };

    window.openProjectMemberTools = function (openModal) {
        const memberTabButton = document.getElementById('projectMembersTabBtn');
        if (memberTabButton) {
            bootstrap.Tab.getOrCreateInstance(memberTabButton).show();
        }

        if (openModal) {
            const modalElement = document.getElementById('addMemberModal');
            if (modalElement) {
                const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
                setTimeout(() => modal.show(), 120);
            }
        }
    };

    if (overlay) overlay.addEventListener('click', window.closeTaskPanel);
    document.addEventListener('keydown', e => {
        if (e.key === 'Escape') window.closeTaskPanel();
    });

    bindTaskPanel(document);

    const createTaskModal = document.getElementById('createTaskModal');
    if (createTaskModal) {
        createTaskModal.addEventListener('hidden.bs.modal', function () {
            const form = this.querySelector('form');
            if (form) form.reset();
        });
    }

    const initialTaskId = new URL(window.location.href).searchParams.get('openTaskId');
    if (initialTaskId) {
        window.openTaskPanel(initialTaskId);
    }
});

function bindTaskPanel(container = document) {
    if (!container) return;
    bindSubtaskFormConstraints(container);
    bindPanelStatusHandlers(container);
    initChecklistPanel(container);
    bindTaskPanelForms(container);
}

function bindPanelStatusHandlers(container = document) {
    container.querySelectorAll('.task-status-select').forEach(sel => {
        if (sel.dataset.bound === 'true') return;
        sel.dataset.bound = 'true';
        sel.dataset.previousValue = sel.value;
        sel.dataset.initiallyDisabled = sel.disabled ? 'true' : 'false';

        sel.addEventListener('change', async function () {
            const taskId = this.dataset.taskId;
            const previousValue = this.dataset.previousValue || this.value;
            this.disabled = true;

            try {
                const data = await postUrlEncoded('/ProjectTask/UpdateStatus', {
                    ProjectTaskId: taskId,
                    Status: this.value
                });

                if (!data.success) {
                    throw new Error(data.message || 'Có lỗi xảy ra.');
                }

                applyTaskState(data.task);
                prependTaskUpdate(data.update);
                showPanelNotice(data.message || 'Đã cập nhật trạng thái.', 'success');
                this.dataset.previousValue = this.value;
            } catch (error) {
                this.value = previousValue;
                showPanelNotice(error.message || 'Không cập nhật được trạng thái.', 'danger');
            } finally {
                if (this.dataset.initiallyDisabled !== 'true' && this.value !== '3') {
                    this.disabled = false;
                }
            }
        });
    });
}

async function updateProgress(e, taskId) {
    e.preventDefault();
    const form = e.currentTarget;
    const slider = form?.querySelector('input[type="range"]') || document.getElementById('progressSlider');
    const button = form?.querySelector('button[type="submit"]');
    let latestTask = null;

    if (!slider) {
        showPanelNotice('Không tìm thấy thanh tiến độ.', 'danger');
        return;
    }

    setButtonBusy(button, true, 'Đang lưu...');
    try {
        const data = await postUrlEncoded('/ProjectTask/UpdateProgress', {
            ProjectTaskId: taskId,
            Progress: slider.value
        });

        if (!data.success) {
            throw new Error(data.message || 'Không cập nhật được tiến độ.');
        }

        applyTaskState(data.task);
        latestTask = data.task;
        prependTaskUpdate(data.update);
        showPanelNotice(data.message || 'Đã cập nhật tiến độ.', 'success');
    } catch (error) {
        showPanelNotice(error.message || 'Không cập nhật được tiến độ.', 'danger');
    } finally {
        setButtonBusy(button, false);
        if (Boolean(readPayload(latestTask, 'isDone', 'IsDone'))) {
            markPanelDoneLocked(getTaskPanelContent());
        }
    }
}

function quickMarkTaskDone(taskId) {
    const content = getTaskPanelContent();
    const statusSelect = content?.querySelector(`.task-status-select[data-task-id="${taskId}"]`)
        || document.querySelector(`.task-status-select[data-task-id="${taskId}"]`);

    if (!statusSelect) {
        showPanelNotice('Không tìm thấy trạng thái của task.', 'danger');
        return;
    }

    const doneOption = Array.from(statusSelect.options).find(option => option.value === '3');
    if (doneOption?.disabled) {
        showPanelNotice('Chỉ assignee hoặc Owner được tick hoàn thành.', 'danger');
        return;
    }

    statusSelect.value = '3';
    statusSelect.dispatchEvent(new Event('change', { bubbles: true }));
}

window.updateProgress = updateProgress;
window.quickMarkTaskDone = quickMarkTaskDone;

function initChecklistPanel(container = document) {
    if (!container) return;

    container.querySelectorAll('.checklist-toggle').forEach(cb => {
        if (cb.dataset.bound === 'true') return;
        cb.dataset.bound = 'true';

        cb.addEventListener('change', async function () {
            const itemId = this.dataset.itemId;
            const row = this.closest('.checklist-item');

            if (!this.checked) {
                this.checked = true;
                showPanelNotice('Checklist đã hoàn thành nên không thể bỏ tick.', 'warning');
                return;
            }

            this.disabled = true;
            try {
                const data = await postUrlEncoded('/ProjectTask/ToggleChecklistItem', { id: itemId });
                if (!data.success) {
                    throw new Error(data.message || 'Không cập nhật được checklist.');
                }

                row?.classList.add('done');
                row?.querySelector('label')?.classList.add('text-decoration-line-through', 'text-muted');
                row?.querySelector('.checklist-delete-btn')?.remove();
                applyTaskState(data.task);
                showPanelNotice(data.message || 'Đã cập nhật checklist.', 'success');
            } catch (error) {
                this.checked = false;
                this.disabled = false;
                showPanelNotice(error.message || 'Không cập nhật được checklist.', 'danger');
            }
        });
    });

    container.querySelectorAll('.checklist-delete-btn').forEach(btn => {
        if (btn.dataset.bound === 'true') return;
        btn.dataset.bound = 'true';

        btn.addEventListener('click', async function () {
            if (this.dataset.confirming !== 'true') {
                this.dataset.confirming = 'true';
                showPanelNotice('Bấm lại nút xoá checklist để xác nhận.', 'warning');
                window.setTimeout(() => delete this.dataset.confirming, 4000);
                return;
            }

            const itemId = this.dataset.itemId;
            const row = this.closest('.checklist-item');
            this.disabled = true;

            try {
                const data = await postUrlEncoded('/ProjectTask/DeleteChecklistItem', { id: itemId });
                if (!data.success) {
                    throw new Error(data.message || 'Không xoá được checklist.');
                }

                row?.remove();
                applyTaskState(data.task);
                syncChecklistEmptyState();
                showPanelNotice(data.message || 'Đã xoá checklist.', 'success');
            } catch (error) {
                this.disabled = false;
                showPanelNotice(error.message || 'Không xoá được checklist.', 'danger');
            }
        });
    });

    container.querySelectorAll('#addChecklistForm').forEach(addChecklistForm => {
        if (addChecklistForm.dataset.bound === 'true') return;
        addChecklistForm.dataset.bound = 'true';

        addChecklistForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            const button = this.querySelector('button[type="submit"]');
            setButtonBusy(button, true, 'Đang thêm...');

            try {
                const data = await postUrlEncoded('/ProjectTask/AddChecklistItem', formToUrlSearchParams(this));
                if (!data.success) {
                    throw new Error(data.message || 'Không thêm được checklist.');
                }

                const list = document.getElementById('checklistContainer');
                list?.querySelector('.checklist-empty')?.remove();
                list?.appendChild(createChecklistItemElement(data.item));
                this.reset();
                initChecklistPanel(list || document);
                applyTaskState(data.task);
                showPanelNotice(data.message || 'Đã thêm checklist.', 'success');
            } catch (error) {
                showPanelNotice(error.message || 'Không thêm được checklist.', 'danger');
            } finally {
                setButtonBusy(button, false);
            }
        });
    });
}

function bindSubtaskFormConstraints(container = document) {
    if (!container) return;

    container.querySelectorAll('.task-subtask-create-form').forEach(form => {
        if (form.dataset.bound === 'true') return;
        form.dataset.bound = 'true';

        const startInput = form.querySelector('input[name="StartDate"]');
        const dueInput = form.querySelector('input[name="DueDate"]');
        const hint = form.querySelector('.task-subtask-date-hint');
        const parentStart = form.dataset.parentStart || '';
        const parentDue = form.dataset.parentDue || '';

        if (!startInput || !dueInput) return;

        const formatDate = value => {
            if (!value) return '';
            const [year, month, day] = value.split('-');
            return `${day}/${month}/${year}`;
        };

        const minDate = (left, right) => {
            if (!left) return right;
            if (!right) return left;
            return left > right ? left : right;
        };

        const maxDate = (left, right) => {
            if (!left) return right;
            if (!right) return left;
            return left < right ? left : right;
        };

        const sync = () => {
            const effectiveStartMin = minDate(parentStart, '');
            const effectiveStartMax = maxDate(parentDue, dueInput.value || '');
            const effectiveDueMin = minDate(parentStart, startInput.value || '');
            const effectiveDueMax = maxDate(parentDue, '');

            if (effectiveStartMin) {
                startInput.min = effectiveStartMin;
            } else {
                startInput.removeAttribute('min');
            }

            if (effectiveStartMax) {
                startInput.max = effectiveStartMax;
            } else {
                startInput.removeAttribute('max');
            }

            if (effectiveDueMin) {
                dueInput.min = effectiveDueMin;
            } else {
                dueInput.removeAttribute('min');
            }

            if (effectiveDueMax) {
                dueInput.max = effectiveDueMax;
            } else {
                dueInput.removeAttribute('max');
            }

            if (hint && parentStart && parentDue) {
                const message = startInput.value && dueInput.value
                    ? `Subtask phải nằm trong khoảng ${formatDate(parentStart)} - ${formatDate(parentDue)}.`
                    : `Chọn ngày trong khoảng ${formatDate(parentStart)} - ${formatDate(parentDue)}.`;
                hint.textContent = message;
            }
        };

        startInput.addEventListener('change', sync);
        dueInput.addEventListener('change', sync);
        sync();
    });
}

function bindTaskPanelForms(container = document) {
    if (!container) return;

    container.querySelectorAll('.task-update-form').forEach(form => {
        if (form.dataset.ajaxBound === 'true') return;
        form.dataset.ajaxBound = 'true';

        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            if (!validateTaskUpdateFiles(this)) return;

            const button = this.querySelector('button[type="submit"]');
            setButtonBusy(button, true, 'Đang gửi...');

            try {
                const data = await postFormData(this.action || '/ProjectTask/PostUpdate', new FormData(this));
                if (!data.success) {
                    throw new Error(data.message || 'Không gửi được cập nhật công việc.');
                }

                prependTaskUpdate(data.update, data.updateCount);
                this.reset();
                showPanelNotice(data.message || 'Đã gửi cập nhật công việc.', 'success');
            } catch (error) {
                showPanelNotice(error.message || 'Không gửi được cập nhật công việc.', 'danger');
            } finally {
                setButtonBusy(button, false);
            }
        });
    });

    container.querySelectorAll('.task-assign-members-form').forEach(form => {
        if (form.dataset.ajaxBound === 'true') return;
        form.dataset.ajaxBound = 'true';

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            const button = this.querySelector('button[type="submit"]');
            setButtonBusy(button, true, 'Đang lưu...');

            try {
                const data = await postUrlEncoded(this.action || '/ProjectTask/AssignMembers', formToUrlSearchParams(this));
                if (!data.success) {
                    throw new Error(data.message || 'Không lưu được phân công.');
                }

                updateAssigneeChips(data.assignees || []);
                syncCompletePermission(Boolean(data.canCompleteTask));
                prependTaskUpdate(data.update, data.updateCount);
                showPanelNotice(data.message || 'Đã lưu phân công.', 'success');
            } catch (error) {
                showPanelNotice(error.message || 'Không lưu được phân công.', 'danger');
            } finally {
                setButtonBusy(button, false);
            }
        });
    });

    container.querySelectorAll('.task-subtask-create-form').forEach(form => {
        if (form.dataset.ajaxBound === 'true') return;
        form.dataset.ajaxBound = 'true';

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            const button = this.querySelector('button[type="submit"]');
            setButtonBusy(button, true, 'Đang tạo...');

            try {
                const data = await postUrlEncoded(this.action || '/ProjectTask/Create', formToUrlSearchParams(this));
                if (!data.success) {
                    throw new Error(data.message || 'Không tạo được subtask.');
                }

                appendSubtaskCard(this, data.task);
                this.reset();
                this.querySelector('input[name="StartDate"]')?.dispatchEvent(new Event('change', { bubbles: true }));
                showPanelNotice(data.message || 'Tạo subtask thành công.', 'success');
            } catch (error) {
                showPanelNotice(error.message || 'Không tạo được subtask.', 'danger');
            } finally {
                setButtonBusy(button, false);
            }
        });
    });
}

async function postUrlEncoded(url, data) {
    const params = data instanceof URLSearchParams ? data : objectToUrlSearchParams(data);
    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getRequestVerificationToken(),
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: params.toString()
    });

    return parseJsonResponse(response);
}

async function postFormData(url, formData) {
    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'RequestVerificationToken': getRequestVerificationToken(),
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: formData
    });

    return parseJsonResponse(response);
}

async function parseJsonResponse(response) {
    const text = await response.text();
    let data = null;

    if (text) {
        try {
            data = JSON.parse(text);
        } catch {
            data = null;
        }
    }

    if (!response.ok) {
        throw new Error(data?.message || text || `HTTP ${response.status}`);
    }

    return data || {};
}

function objectToUrlSearchParams(data) {
    const params = new URLSearchParams();
    Object.entries(data || {}).forEach(([key, value]) => {
        if (value === undefined || value === null) return;

        if (Array.isArray(value)) {
            value.forEach(item => params.append(key, item));
            return;
        }

        params.append(key, value);
    });
    return params;
}

function formToUrlSearchParams(form) {
    const params = new URLSearchParams();
    new FormData(form).forEach((value, key) => {
        if (value instanceof File) return;
        params.append(key, value);
    });
    return params;
}

function validateTaskUpdateFiles(form) {
    const input = form.querySelector('input[type="file"][name="Attachments"]');
    const fileCount = input?.files?.length || 0;

    if (fileCount > 2) {
        showPanelNotice(`Chỉ được upload tối đa 2 files. Bạn đã chọn ${fileCount} files.`, 'danger');
        return false;
    }

    return true;
}

function getTaskPanelContent() {
    return document.getElementById('taskDetailContent');
}

function showPanelNotice(message, type = 'info') {
    const area = document.getElementById('taskPanelNotice');
    if (!area) {
        showToast(message, type);
        return;
    }

    window.clearTimeout(taskPanelNoticeTimer);
    area.innerHTML = '';
    area.classList.add('has-message');

    const notice = document.createElement('div');
    notice.className = `task-panel-notice ${type}`;

    const text = document.createElement('span');
    text.textContent = message;

    const close = document.createElement('button');
    close.type = 'button';
    close.setAttribute('aria-label', 'Đóng thông báo');
    close.innerHTML = '<i class="bi bi-x-lg"></i>';
    close.addEventListener('click', () => clearPanelNotice());

    notice.append(text, close);
    area.appendChild(notice);

    taskPanelNoticeTimer = window.setTimeout(clearPanelNotice, type === 'danger' ? 8000 : 5000);
}

function clearPanelNotice() {
    const area = document.getElementById('taskPanelNotice');
    if (!area) return;
    area.innerHTML = '';
    area.classList.remove('has-message');
}

function applyTaskState(task) {
    if (!task) return;

    const content = getTaskPanelContent();
    if (!content) return;

    const status = readPayload(task, 'status', 'Status');
    const statusLabel = readPayload(task, 'statusLabel', 'StatusLabel');
    const statusCss = readPayload(task, 'statusCss', 'StatusCss');
    const progress = Number(readPayload(task, 'progress', 'Progress') || 0);
    const progressText = readPayload(task, 'progressText', 'ProgressText') || formatPercentText(progress);
    const progressCss = readPayload(task, 'progressCss', 'ProgressCss') || progressColor(progress);
    const isDone = Boolean(readPayload(task, 'isDone', 'IsDone'));

    const headerStatus = content.querySelector('.task-header-status');
    if (headerStatus) {
        replaceClassPrefix(headerStatus, 'task-status-', statusCss);
        if (statusLabel) headerStatus.textContent = statusLabel;
    }

    const statusSelect = content.querySelector('.task-status-select');
    if (statusSelect && status !== undefined && status !== null) {
        statusSelect.value = String(status);
        statusSelect.dataset.previousValue = String(status);
        if (isDone) statusSelect.disabled = true;
    }

    const progressValue = content.querySelector('.task-progress-value');
    if (progressValue) progressValue.textContent = progressText;

    const progressBar = content.querySelector('.task-progress-bar');
    if (progressBar) {
        progressBar.style.width = formatPercentCss(progress);
        replaceProgressClass(progressBar, progressCss);
    }

    const slider = content.querySelector('#progressSlider');
    const sliderValue = content.querySelector('#progressVal');
    if (slider) slider.value = Math.round(progress);
    if (sliderValue) sliderValue.textContent = formatPercentText(progress);

    updateChecklistCount(task);

    if (isDone) {
        markPanelDoneLocked(content);
    }
}

function markPanelDoneLocked(content) {
    if (!content) return;

    content.querySelector('.task-complete-btn')?.remove();
    content.querySelectorAll('.checklist-toggle').forEach(cb => cb.disabled = true);
    content.querySelector('#addChecklistForm')?.remove();

    const manualProgressForm = content.querySelector('form[onsubmit*="updateProgress"]');
    manualProgressForm?.querySelectorAll('input, button').forEach(control => control.disabled = true);

    const statusSelect = content.querySelector('.task-status-select');
    if (statusSelect) {
        statusSelect.disabled = true;
        ensureTaskLockNote(statusSelect, 'Đã hoàn thành, không thể hoàn tác.');
    }
}

function updateChecklistCount(task) {
    const label = getTaskPanelContent()?.querySelector('.task-checklist-count');
    if (!label) return;

    const done = readPayload(task, 'checklistDone', 'ChecklistDone');
    const total = readPayload(task, 'checklistTotal', 'ChecklistTotal');
    if (done === undefined || total === undefined) return;

    label.textContent = `Checklist (${done}/${total})`;
}

function syncChecklistEmptyState() {
    const container = document.getElementById('checklistContainer');
    if (!container) return;

    const hasItems = container.querySelector('.checklist-item');
    const empty = container.querySelector('.checklist-empty');

    if (hasItems) {
        empty?.remove();
        return;
    }

    if (!empty) {
        const message = document.createElement('div');
        message.className = 'checklist-empty text-muted small';
        message.textContent = 'Chưa có checklist cho task này.';
        container.appendChild(message);
    }
}

function createChecklistItemElement(item) {
    const itemId = readPayload(item, 'projectTaskChecklistItemId', 'ProjectTaskChecklistItemId');
    const title = readPayload(item, 'title', 'Title') || 'Checklist';

    const row = document.createElement('div');
    row.className = 'checklist-item';
    if (itemId) row.dataset.checklistItemId = itemId;

    const input = document.createElement('input');
    input.type = 'checkbox';
    input.className = 'checklist-toggle';
    input.dataset.itemId = itemId;

    const label = document.createElement('label');
    label.textContent = title;

    const deleteButton = document.createElement('button');
    deleteButton.type = 'button';
    deleteButton.className = 'btn btn-sm btn-outline-danger p-0 px-1 checklist-delete-btn';
    deleteButton.dataset.itemId = itemId;
    deleteButton.style.fontSize = '0.72rem';
    deleteButton.innerHTML = '<i class="bi bi-trash"></i>';

    row.append(input, label, deleteButton);
    return row;
}

function updateAssigneeChips(assignees) {
    const form = getTaskPanelContent()?.querySelector('.task-assign-members-form');
    if (!form) return;

    const section = form.closest('.task-section-card');
    if (!section) return;

    section.querySelector('.task-assignee-chip-list')?.remove();
    section.querySelector('.task-assignee-empty')?.remove();

    if (!assignees.length) {
        const empty = document.createElement('div');
        empty.className = 'task-assignee-empty text-muted small mb-3';
        empty.textContent = 'Chưa có ai được giao task này.';
        section.insertBefore(empty, form);
        return;
    }

    const list = document.createElement('div');
    list.className = 'task-assignee-chip-list d-flex flex-wrap gap-1 mb-3';

    assignees.forEach(name => {
        const chip = document.createElement('span');
        chip.className = 'd-flex align-items-center gap-1 px-2 py-1 rounded-pill bg-light';
        chip.style.fontSize = '0.8rem';

        const avatar = document.createElement('span');
        avatar.className = 'assignee-avatar';
        avatar.style.width = '20px';
        avatar.style.height = '20px';
        avatar.style.fontSize = '0.6rem';
        avatar.textContent = name ? name[0].toUpperCase() : '?';

        chip.append(avatar, document.createTextNode(name));
        list.appendChild(chip);
    });

    section.insertBefore(list, form);
}

function syncCompletePermission(canCompleteTask) {
    const content = getTaskPanelContent();
    const statusSelect = content?.querySelector('.task-status-select');
    if (!content || !statusSelect) return;

    const isDone = statusSelect.value === '3';
    content.querySelectorAll('.checklist-toggle:not(:checked)').forEach(cb => {
        cb.disabled = isDone || !canCompleteTask;
    });

    const doneOption = Array.from(statusSelect.options).find(option => option.value === '3');
    if (doneOption && !isDone) doneOption.disabled = !canCompleteTask;

    const existingButton = content.querySelector('.task-complete-btn');
    if (!canCompleteTask) {
        existingButton?.remove();
        if (!isDone) ensureTaskLockNote(statusSelect, 'Chỉ assignee hoặc Owner được tick hoàn thành.');
        return;
    }

    removePermissionLockNote(statusSelect);
    if (isDone || existingButton) return;

    const progressActions = content.querySelector('.task-progress-value')?.parentElement;
    if (!progressActions) return;

    const taskId = statusSelect.dataset.taskId || currentOpenTaskId;
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'btn btn-sm btn-success task-complete-btn';
    button.innerHTML = '<i class="bi bi-check2-circle me-1"></i>Đánh dấu hoàn thành';
    button.addEventListener('click', () => quickMarkTaskDone(taskId));
    progressActions.appendChild(button);
}

function ensureTaskLockNote(statusSelect, message) {
    const row = statusSelect.closest('.d-flex');
    if (!row) return;

    let note = row.querySelector('.task-lock-note');
    if (!note) {
        note = document.createElement('span');
        note.className = 'task-lock-note';
        row.appendChild(note);
    }
    note.textContent = message;
}

function removePermissionLockNote(statusSelect) {
    const note = statusSelect.closest('.d-flex')?.querySelector('.task-lock-note');
    if (note?.textContent.includes('Chỉ assignee')) note.remove();
}

function prependTaskUpdate(update, explicitCount) {
    if (!update) return;

    const card = getTaskPanelContent()?.querySelector('.task-worklog-card');
    if (!card) return;

    const updateId = readPayload(update, 'projectTaskUpdateId', 'ProjectTaskUpdateId');
    if (updateId && card.querySelector(`.task-update-item[data-update-id="${updateId}"]`)) {
        if (explicitCount !== undefined && explicitCount !== null) setWorklogCount(explicitCount);
        return;
    }

    let list = card.querySelector('.task-update-list');
    if (!list) {
        list = document.createElement('div');
        list.className = 'task-update-list';
        const form = card.querySelector('.task-update-form');
        card.querySelector('.task-worklog-empty')?.remove();
        card.insertBefore(list, form || null);
    }

    list.prepend(createTaskUpdateElement(update));

    const nextCount = explicitCount ?? (readCurrentWorklogCount() + 1);
    setWorklogCount(nextCount);
    if (nextCount > 4 || list.children.length > 4) {
        list.classList.add('is-scrollable');
    }
}

function createTaskUpdateElement(update) {
    const updateId = readPayload(update, 'projectTaskUpdateId', 'ProjectTaskUpdateId');
    const senderName = readPayload(update, 'senderName', 'SenderName') || 'Hệ thống';
    const avatarText = readPayload(update, 'avatar', 'Avatar') || '?';
    const content = readPayload(update, 'content', 'Content') || '';
    const createdDate = readPayload(update, 'createdDate', 'CreatedDate') || '';
    const statusLabel = readPayload(update, 'statusLabel', 'StatusLabel');
    const statusCss = readPayload(update, 'statusCss', 'StatusCss');
    const progressText = readPayload(update, 'progressText', 'ProgressText');
    const attachments = readPayload(update, 'attachments', 'Attachments') || [];

    const item = document.createElement('div');
    item.className = 'task-update-item';
    if (updateId) item.dataset.updateId = updateId;

    const avatar = document.createElement('div');
    avatar.className = 'task-update-avatar';
    avatar.textContent = avatarText;

    const body = document.createElement('div');
    body.className = 'task-update-content';

    const meta = document.createElement('div');
    meta.className = 'task-update-meta';

    const strong = document.createElement('strong');
    strong.textContent = senderName;
    const time = document.createElement('span');
    time.textContent = createdDate;
    meta.append(strong, time);

    const badges = document.createElement('div');
    badges.className = 'task-update-badges';
    if (statusLabel) {
        const status = document.createElement('span');
        status.className = `status-badge ${statusCss || ''}`;
        status.style.fontSize = '0.66rem';
        status.textContent = statusLabel;
        badges.appendChild(status);
    }
    if (progressText) {
        const progress = document.createElement('span');
        progress.className = 'task-snapshot-chip';
        progress.textContent = progressText;
        badges.appendChild(progress);
    }

    const text = document.createElement('p');
    text.className = 'mb-0 mt-2';
    text.style.whiteSpace = 'pre-wrap';
    text.textContent = content;

    body.append(meta, badges, text);

    if (attachments.length) {
        const attachmentList = document.createElement('div');
        attachmentList.className = 'task-update-attachments';
        attachments.forEach(file => attachmentList.appendChild(createAttachmentElement(file)));
        body.appendChild(attachmentList);
    }

    item.append(avatar, body);
    return item;
}

function createAttachmentElement(file) {
    const link = document.createElement('a');
    link.className = 'task-attachment-chip';
    link.href = readPayload(file, 'url', 'Url') || '#';

    const icon = document.createElement('i');
    icon.className = 'bi bi-paperclip';

    const name = document.createElement('span');
    name.textContent = readPayload(file, 'fileName', 'FileName') || 'Tệp đính kèm';

    const size = document.createElement('small');
    size.textContent = readPayload(file, 'size', 'Size') || '';

    link.append(icon, name, size);
    return link;
}

function appendSubtaskCard(form, task) {
    if (!task) return;

    const section = form.closest('.task-section-card');
    if (!section) return;

    let list = section.querySelector('.task-subtask-list');
    if (!list) {
        list = document.createElement('div');
        list.className = 'task-subtask-list';
        section.querySelector('.task-subtask-empty')?.remove();
        section.insertBefore(list, form);
    }

    list.appendChild(createSubtaskCard(task));
    const count = list.querySelectorAll('.task-subtask-card').length;
    const label = section.querySelector('.task-subtask-count');
    if (label) label.textContent = `Subtask (${count})`;
}

function createSubtaskCard(task) {
    const taskId = readPayload(task, 'projectTaskId', 'ProjectTaskId');
    const title = readPayload(task, 'title', 'Title') || 'Subtask';
    const statusLabel = readPayload(task, 'statusLabel', 'StatusLabel') || '';
    const statusCss = readPayload(task, 'statusCss', 'StatusCss') || '';
    const progress = Number(readPayload(task, 'progress', 'Progress') || 0);
    const dueDate = readPayload(task, 'dueDate', 'DueDate');

    const card = document.createElement('div');
    card.className = 'task-subtask-card';
    if (taskId) card.dataset.subtaskId = taskId;

    const wrap = document.createElement('div');
    wrap.className = 'd-flex justify-content-between align-items-start gap-2';

    const left = document.createElement('div');
    left.style.minWidth = '0';

    const link = document.createElement('button');
    link.type = 'button';
    link.className = 'task-subtask-link';
    link.textContent = title;
    link.addEventListener('click', () => openTaskPanel(taskId));

    const meta = document.createElement('div');
    meta.className = 'd-flex align-items-center gap-2 mt-1 flex-wrap';

    const status = document.createElement('span');
    status.className = `status-badge ${statusCss}`;
    status.style.fontSize = '0.68rem';
    status.textContent = statusLabel;

    const progressText = document.createElement('span');
    progressText.className = 'text-muted small';
    progressText.textContent = formatPercentText(progress);

    meta.append(status, progressText);
    if (dueDate) {
        const due = document.createElement('span');
        due.className = 'text-muted small';
        due.innerHTML = '<i class="bi bi-calendar3 me-1"></i>';
        due.append(document.createTextNode(dueDate));
        meta.appendChild(due);
    }

    left.append(link, meta);

    const actions = document.createElement('div');
    actions.className = 'task-subtask-actions';

    wrap.append(left, actions);
    card.appendChild(wrap);
    return card;
}

function setWorklogCount(count) {
    const label = getTaskPanelContent()?.querySelector('.task-worklog-count');
    if (label) label.textContent = `${count} cập nhật`;
}

function readCurrentWorklogCount() {
    const text = getTaskPanelContent()?.querySelector('.task-worklog-count')?.textContent || '0';
    const match = text.match(/\d+/);
    return match ? Number(match[0]) : 0;
}

function readPayload(source, ...keys) {
    if (!source) return undefined;
    for (const key of keys) {
        if (source[key] !== undefined && source[key] !== null) {
            return source[key];
        }
    }
    return undefined;
}

function replaceClassPrefix(element, prefix, nextClass) {
    Array.from(element.classList)
        .filter(className => className.startsWith(prefix))
        .forEach(className => element.classList.remove(className));

    if (nextClass) element.classList.add(nextClass);
}

function replaceProgressClass(element, nextClass) {
    ['bg-success', 'bg-info', 'bg-warning', 'bg-danger'].forEach(className => element.classList.remove(className));
    if (nextClass) element.classList.add(nextClass);
}

function progressColor(progress) {
    if (progress >= 100) return 'bg-success';
    if (progress >= 60) return 'bg-info';
    if (progress >= 30) return 'bg-warning';
    return 'bg-danger';
}

function formatPercentText(value) {
    return `${Math.round(Number(value) || 0)}%`;
}

function formatPercentCss(value) {
    const clamped = Math.max(0, Math.min(100, Number(value) || 0));
    return `${clamped}%`;
}

function setButtonBusy(button, busy, busyText = 'Đang xử lý...') {
    if (!button) return;

    if (busy) {
        button.dataset.originalHtml = button.innerHTML;
        button.dataset.wasDisabled = button.disabled ? 'true' : 'false';
        button.disabled = true;
        button.textContent = busyText;
        return;
    }

    if (button.dataset.originalHtml) {
        button.innerHTML = button.dataset.originalHtml;
    }
    button.disabled = button.dataset.wasDisabled === 'true';
    delete button.dataset.originalHtml;
    delete button.dataset.wasDisabled;
}

function getRequestVerificationToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}

function clearOpenTaskQueryParam() {
    const url = new URL(window.location.href);
    if (!url.searchParams.has('openTaskId')) return;
    url.searchParams.delete('openTaskId');
    window.history.replaceState({}, '', url.toString());
}

function showToast(message, type = 'info') {
    const container = document.getElementById('toastContainer') || createToastContainer();
    const id = 'toast_' + Date.now();
    const html = `
    <div id="${id}" class="toast align-items-center text-bg-${type} border-0 show" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    </div>`;
    container.insertAdjacentHTML('beforeend', html);
    setTimeout(() => { document.getElementById(id)?.remove(); }, 4000);
}

function createToastContainer() {
    const div = document.createElement('div');
    div.id = 'toastContainer';
    div.className = 'toast-container position-fixed bottom-0 end-0 p-3';
    div.style.zIndex = '9999';
    document.body.appendChild(div);
    return div;
}
