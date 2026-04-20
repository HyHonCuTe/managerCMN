/* ── Project Management JS ── */

let currentOpenTaskId = null;

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
                bindSubtaskFormConstraints(content);
                initChecklistPanel();
                bindPanelStatusHandlers();
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

    bindPanelStatusHandlers();

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

function bindPanelStatusHandlers() {
    document.querySelectorAll('.task-status-select').forEach(sel => {
        if (sel.dataset.bound === 'true') return;
        sel.dataset.bound = 'true';

        sel.addEventListener('change', function () {
            const taskId = this.dataset.taskId;
            fetch('/ProjectTask/UpdateStatus', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': getRequestVerificationToken()
                },
                body: `ProjectTaskId=${taskId}&Status=${this.value}`
            })
                .then(r => r.json())
                .then(data => {
                    if (data.success) {
                        reloadProjectPageForTask(taskId);
                    } else {
                        showToast(data.message || 'Có lỗi xảy ra.', 'danger');
                    }
                })
                .catch(() => showToast('Không cập nhật được trạng thái.', 'danger'));
        });
    });
}

function updateProgress(e, taskId) {
    e.preventDefault();
    const slider = document.getElementById('progressSlider');
    if (!slider) {
        showToast('Không tìm thấy thanh tiến độ.', 'danger');
        return;
    }

    const val = slider.value;
    fetch('/ProjectTask/UpdateProgress', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getRequestVerificationToken()
        },
        body: `ProjectTaskId=${taskId}&Progress=${val}`
    })
        .then(r => r.json())
        .then(d => {
            if (d.success) {
                reloadProjectPageForTask(taskId);
            } else {
                showToast(d.message || 'Không cập nhật được tiến độ.', 'danger');
            }
        })
        .catch(() => showToast('Không cập nhật được tiến độ.', 'danger'));
}

function quickMarkTaskDone(taskId) {
    const statusSelect = document.querySelector(`.task-status-select[data-task-id="${taskId}"]`);
    if (!statusSelect) {
        showToast('Không tìm thấy trạng thái của task.', 'danger');
        return;
    }

    statusSelect.value = '3';
    statusSelect.dispatchEvent(new Event('change'));
}

window.updateProgress = updateProgress;
window.quickMarkTaskDone = quickMarkTaskDone;

function initChecklistPanel() {
    document.querySelectorAll('.checklist-toggle').forEach(cb => {
        if (cb.dataset.bound === 'true') return;
        cb.dataset.bound = 'true';

        cb.addEventListener('change', function () {
            const itemId = this.dataset.itemId;
            fetch('/ProjectTask/ToggleChecklistItem', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': getRequestVerificationToken()
                },
                body: `id=${itemId}`
            })
                .then(r => r.json())
                .then(data => {
                    if (!data.success) {
                        showToast(data.message || 'Lỗi', 'danger');
                        return;
                    }

                    reloadProjectPageForTask(currentOpenTaskId);
                })
                .catch(() => showToast('Không cập nhật được checklist.', 'danger'));
        });
    });

    document.querySelectorAll('.checklist-delete-btn').forEach(btn => {
        if (btn.dataset.bound === 'true') return;
        btn.dataset.bound = 'true';

        btn.addEventListener('click', function () {
            if (!confirm('Xoá mục này?')) return;
            const itemId = this.dataset.itemId;
            fetch('/ProjectTask/DeleteChecklistItem', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': getRequestVerificationToken()
                },
                body: `id=${itemId}`
            })
                .then(r => r.json())
                .then(data => {
                    if (data.success) {
                        reloadProjectPageForTask(currentOpenTaskId);
                    } else {
                        showToast(data.message || 'Lỗi', 'danger');
                    }
                })
                .catch(() => showToast('Không xoá được checklist.', 'danger'));
        });
    });

    const addChecklistForm = document.getElementById('addChecklistForm');
    if (addChecklistForm && addChecklistForm.dataset.bound !== 'true') {
        addChecklistForm.dataset.bound = 'true';
        addChecklistForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const formData = new FormData(this);
            const params = new URLSearchParams(formData);
            fetch('/ProjectTask/AddChecklistItem', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': getRequestVerificationToken()
                },
                body: params.toString()
            })
                .then(r => r.json())
                .then(data => {
                    if (data.success) {
                        reloadProjectPageForTask(currentOpenTaskId);
                    } else {
                        showToast(data.message || 'Lỗi', 'danger');
                    }
                })
                .catch(() => showToast('Không thêm được checklist.', 'danger'));
        });
    }
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
