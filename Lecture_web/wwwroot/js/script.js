// Modal functions
function showModal(modalId) {
    document.getElementById(modalId).classList.add('active');
}

function closeModal(modalId) {
    document.getElementById(modalId).classList.remove('active');
}

// Prevent form submission
document.querySelectorAll('form').forEach(form => {
    form.addEventListener('submit', function (e) {
        e.preventDefault();
    });
});

// Close modal when clicking outside
window.addEventListener('click', function (event) {
    if (event.target.classList.contains('modal')) {
        event.target.classList.remove('active');
    }
});

function showAddClassModal() {
    showModal('classModal');
}

function showAddStudentModal() {
    showModal('addStudentModal');
}

function showAddLectureModal() {
    showModal('lectureModal');
}

function showAddMaterialModal() {
    showModal('materialModal');
}

function showAddQuizModal() {
    showModal('quizModal');
}

// Tab switching
function switchTab(tabName) {
    const tabs = document.querySelectorAll('.tab-btn');
    const contents = document.querySelectorAll('.tab-content');

    tabs.forEach(tab => tab.classList.remove('active'));
    contents.forEach(content => content.classList.remove('active'));

    document.querySelector(`[onclick="switchTab('${tabName}')"]`).classList.add('active');
    document.getElementById(`${tabName}Tab`).classList.add('active');
}

// Class management functions
function editClass(classId) {
    showModal('classModal');
}

function hideClassDetails() {
    document.getElementById('classDetailsSection').style.display = 'none';
}

// Student management functions
function searchStudents() {
    const searchText = document.getElementById('studentSearch').value.toLowerCase();
    const rows = document.querySelectorAll('.student-table tbody tr');

    rows.forEach(row => {
        const name = row.querySelector('td:nth-child(2)').textContent.toLowerCase();
        if (name.includes(searchText)) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

function filterStudents() {
    const status = document.getElementById('statusFilter').value;
    const rows = document.querySelectorAll('.student-table tbody tr');

    rows.forEach(row => {
        const studentStatus = row.querySelector('.status-badge').classList.contains('active') ? 'active' : 'inactive';
        if (status === 'all' || status === studentStatus) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

function addStudent() {
    const email = document.getElementById('studentEmail').value;
    if (!email) {
        alert('Vui lòng nhập email sinh viên');
        return;
    }
    if (!validateEmail(email)) {
        alert('Email không hợp lệ');
        return;
    }
    // Add student to list
    const studentItems = document.querySelector('.student-items');
    const studentItem = document.createElement('div');
    studentItem.className = 'student-item';
    studentItem.innerHTML = `
          <div class="student-info">
              <h4>${email.split('@')[0]}</h4>
              <span>${email}</span>
          </div>
          <div class="student-status pending">
              <i class="fas fa-clock"></i>
              Đang chờ xác nhận
          </div>
          <button class="btn-danger" onclick="removeStudent(this)">
              <i class="fas fa-times"></i>
          </button>
      `;
    studentItems.appendChild(studentItem);
    document.getElementById('studentEmail').value = '';
}

function removeStudent(button) {
    if (button.closest('.student-item')) {
        button.closest('.student-item').remove();
    } else {
        if (confirm('Bạn có chắc chắn muốn xóa sinh viên này khỏi lớp học?')) {
            // Remove student from class
            alert('Đã xóa sinh viên khỏi lớp học');
        }
    }
}

function validateEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

function sendInvitations() {
    const pendingStudents = document.querySelectorAll('.student-status.pending');
    if (pendingStudents.length === 0) {
        alert('Không có sinh viên nào để gửi lời mời');
        return;
    }
    // Send invitations
    alert('Đã gửi lời mời tham gia lớp học cho các sinh viên');
    closeModal('addStudentModal');
}

// Content management functions
function editLecture(lectureId) {
    showModal('lectureModal');
}

function deleteLecture(lectureId) {
    if (confirm('Bạn có chắc chắn muốn xóa bài giảng này?')) {
        // Logic to delete lecture
    }
}

function editMaterial(materialId) {
    showModal('materialModal');
}

function deleteMaterial(materialId) {
    if (confirm('Bạn có chắc chắn muốn xóa tài liệu này?')) {
        // Logic to delete material
    }
}

// User menu functions
function toggleUserMenu() {
    document.querySelector('.user-dropdown').classList.toggle('active');
}

// Close dropdown when clicking outside
document.addEventListener('click', function (event) {
    const userProfile = document.querySelector('.user-profile');
    const dropdown = document.querySelector('.user-dropdown');
    if (!userProfile.contains(event.target)) {
        dropdown.classList.remove('active');
    }
});

function logout() {
    if (confirm('Bạn có chắc chắn muốn đăng xuất?')) {
        window.location.href = '../login.html';
    }
}

function toggleSubMenu(event, anchor) {
    event.preventDefault();
    const submenu = anchor.parentElement.querySelector('.submenu');
    if (submenu) {
        submenu.classList.toggle('open');
    }
}

function selectHocPhan(maHocPhan) {
    // Xử lý hiển thị nội dung học phần bên phải
    alert('Chọn học phần: ' + maHocPhan);
    // Ở đây bạn sẽ load nội dung học phần vào main-content
}

function showLopHocPhan(maHocPhan) {
    // Ở đây bạn sẽ load danh sách lớp học phần vào main-content
    // Ví dụ demo:
    document.querySelector('.main-content').innerHTML = `
        <div class="class-list">
          <div class="class-card">
            <div class="class-info">
              <h3>Lớp học phần 1 (${maHocPhan})</h3>
              <div class="class-meta">
                <span><i class="fas fa-users"></i> 45 sinh viên</span>
              </div>
            </div>
          </div>
          <div class="class-card">
            <div class="class-info">
              <h3>Lớp học phần 2 (${maHocPhan})</h3>
              <div class="class-meta">
                <span><i class="fas fa-users"></i> 40 sinh viên</span>
              </div>
            </div>
          </div>
        </div>
      `;
}

// Modal xác nhận xóa lớp học phần
let classCardToDelete = null;

function deleteClass(button) {
    classCardToDelete = button.closest('.class-card');
    document.getElementById('confirmDeleteModal').classList.add('active');
}

function closeConfirmDelete() {
    document.getElementById('confirmDeleteModal').classList.remove('active');
    classCardToDelete = null;
}

function confirmDeleteClass() {
    if (classCardToDelete) {
        classCardToDelete.remove();
        classCardToDelete = null;
    }
    closeConfirmDelete();
}

// Search
var searchAssignmentInput = document.getElementById('searchAssignmentInput');
if (searchAssignmentInput) {
    searchAssignmentInput.addEventListener('input', function () {
        const value = this.value.toLowerCase();
        document.querySelectorAll('#assignmentTable tbody tr').forEach(row => {
            row.style.display = row.textContent.toLowerCase().includes(value) ? '' : 'none';
        });
    });
}

// Modal functions
function openAddAssignmentModal() {
    document.getElementById('addAssignmentModal').classList.add('active');
}

function closeAddAssignmentModal() {
    document.getElementById('addAssignmentModal').classList.remove('active');
}

// Đóng modal khi click ra ngoài
document.querySelectorAll('.modal').forEach(modal => {
    modal.addEventListener('click', function (e) {
        if (e.target === this) {
            this.classList.remove('active');
        }
    });
});

// Đăng xuất
function logout() {
    if (confirm('Bạn có chắc chắn muốn đăng xuất?')) {
        window.location.href = 'login.html';
    }
}

function toggleSubMenu(event, anchor) {
    event.preventDefault();
    const submenu = anchor.parentElement.querySelector('.submenu');
    if (submenu) {
        submenu.classList.toggle('open');
    }
}

function selectHocPhan(maHocPhan) {
    alert('Chọn học phần: ' + maHocPhan);
    // Xử lý hiển thị nội dung học phần bên phải
}


// Đóng popup chỉnh sửa chương
function closeEditAssignmentModal() {
    document.getElementById('editAssignmentModal').classList.remove('active');
}

// Xử lý lưu chỉnh sửa (demo, bạn có thể thay bằng logic thực tế)
//function saveEditAssignment() {
//    // Lấy dữ liệu từ form
//    const name = document.getElementById('editChapterName').value;
//    const desc = document.getElementById('editChapterDesc').value;
//    const course = document.getElementById('editChapterCourse').value;
//    const classCode = document.getElementById('editChapterClass').value;
//    // TODO: Gửi dữ liệu lên server hoặc cập nhật bảng
//    alert('Đã lưu chỉnh sửa chương: ' + name);
//    closeEditAssignmentModal();
//}


let chapterRowToDelete = null;
document.querySelectorAll('.assignment-actions-btns .btn-danger[title="Xóa"]').forEach((btn) => {
    btn.onclick = function () {
        chapterRowToDelete = btn.closest('tr');
        document.getElementById('confirmDeleteChapterModal').classList.add('active');
    }
});
function closeConfirmDeleteChapter() {
    document.getElementById('confirmDeleteChapterModal').classList.remove('active');
    chapterRowToDelete = null;
}

function confirmDeleteChapter() {
    if (chapterRowToDelete) {
        chapterRowToDelete.remove();
        chapterRowToDelete = null;
    }
    closeConfirmDeleteChapter();
}

//Profile
function toggleUserMenu() {
    document.querySelector('.user-dropdown').classList.toggle('active');
}

// Close dropdown when clicking outside
document.addEventListener('click', function (event) {
    const userProfile = document.querySelector('.user-profile');
    const dropdown = document.querySelector('.user-dropdown');
    if (!userProfile.contains(event.target)) {
        dropdown.classList.remove('active');
    }
});

function logout() {
    if (confirm('Bạn có chắc chắn muốn đăng xuất?')) {
        window.location.href = 'login.html';
    }
}

// Tab switching for content type
function switchContentTab(type) {
    document.querySelectorAll('.content-tab-btn').forEach(btn => btn.classList.remove('active'));
    if (type === 'all') {
        document.querySelectorAll('#contentTable tbody tr').forEach(row => row.style.display = '');
    } else {
        document.querySelectorAll('#contentTable tbody tr').forEach(row => {
            row.style.display = row.getAttribute('data-type') === type ? '' : 'none';
        });
    }
    document.querySelector(`.content-tab-btn[onclick*="${type}"]`).classList.add('active');
}

// Search
var searchInput = document.getElementById('searchInput');
if (searchInput) {
    searchInput.addEventListener('input', function () {
        const value = this.value.toLowerCase();
        document.querySelectorAll('#contentTable tbody tr').forEach(row => {
            row.style.display = row.textContent.toLowerCase().includes(value) ? '' : 'none';
        });
    });
}

// Modal functions
function openAddContentModal() {
    document.getElementById('addContentModal').classList.add('active');
}

function closeAddContentModal() {
    document.getElementById('addContentModal').classList.remove('active');
}

// Đóng modal khi click ra ngoài
document.querySelectorAll('.modal').forEach(modal => {
    modal.addEventListener('click', function (e) {
        if (e.target === this) {
            this.classList.remove('active');
        }
    });
});

// Đăng xuất
function logout() {
    if (confirm('Bạn có chắc chắn muốn đăng xuất?')) {
        window.location.href = 'login.html';
    }
}

function toggleSubMenu(event, anchor) {
    event.preventDefault();
    const submenu = anchor.parentElement.querySelector('.submenu');
    if (submenu) {
        submenu.classList.toggle('open');
    }
}

function selectHocPhan(maHocPhan) {
    alert('Chọn học phần: ' + maHocPhan);
    // Xử lý hiển thị nội dung học phần bên phải
}

// Mở popup chỉnh sửa bài giảng và điền dữ liệu
function openEditContentModal(title = '', desc = '', classCode = '') {
    document.getElementById('editLectureTitle').value = title;
    document.getElementById('editLectureDesc').value = desc;
    document.getElementById('editLectureClass').value = classCode;
    document.getElementById('editContentModal').classList.add('active');
}

// Đóng popup chỉnh sửa bài giảng
function closeEditContentModal() {
    document.getElementById('editContentModal').classList.remove('active');
}

// Xử lý lưu chỉnh sửa (demo, bạn có thể thay bằng logic thực tế)
function saveEditContent() {
    const title = document.getElementById('editLectureTitle').value;
    const desc = document.getElementById('editLectureDesc').value;
    const classCode = document.getElementById('editLectureClass').value;
    alert('Đã lưu chỉnh sửa bài giảng: ' + title);
    closeEditContentModal();
}

// Gắn sự kiện cho nút chỉnh sửa trong bảng bài giảng (chỉ cho bảng bài giảng)
//function bindContentTableEvents() {
//    const table = document.getElementById('contentTable');
//    if (!table) return;
//    table.onclick = function (e) {
//        const btn = e.target.closest('button');
//        if (!btn) return;
//        const row = btn.closest('tr');
//        if (!row) return;
//        // Chỉnh sửa
//        if (btn.classList.contains('btn-secondary') && btn.title === 'Chỉnh sửa') {
//            const title = row.children[0].innerText;
//            const desc = row.children[1].innerText;
//            const classCode = row.children[2].innerText;
//            document.getElementById('editLectureTitle').value = title;
//            document.getElementById('editLectureDesc').value = desc;
//            document.getElementById('editLectureClass').value = classCode;
//            document.getElementById('editContentModal').classList.add('active');
//        }
//        // Xóa
//        //if (btn.classList.contains('btn-danger') && btn.title === 'Xóa') {
//        //    contentRowToDelete = row;
//        //    document.getElementById('confirmDeleteContentModal').classList.add('active');
//        //}
//    }
//}
//document.addEventListener('DOMContentLoaded', bindContentTableEvents);

// Đóng modal xác nhận xóa
function closeConfirmDeleteContent() {
    document.getElementById('confirmDeleteContentModal').classList.remove('active');
    contentRowToDelete = null;
}
// Xác nhận xóa
function confirmDeleteContent() {
    if (contentRowToDelete) {
        contentRowToDelete.remove();
        contentRowToDelete = null;
    }
    closeConfirmDeleteContent();
}

// Đóng popup chỉnh sửa bài giảng
function closeEditContentModal() {
    document.getElementById('editContentModal').classList.remove('active');
}

// User menu
function toggleUserMenu() {
    document.querySelector('.user-dropdown').classList.toggle('active');
}

// Close dropdown when clicking outside
document.addEventListener('click', function (event) {
    const userProfile = document.querySelector('.user-profile');
    const dropdown = document.querySelector('.user-dropdown');
    if (!userProfile.contains(event.target)) {
        dropdown.classList.remove('active');
    }
});

//QuanLybai

// Search
var searchBaiInput = document.getElementById('searchBaiInput');
if (searchBaiInput) {
    searchBaiInput.addEventListener('input', function () {
        const value = this.value.toLowerCase();
        document.querySelectorAll('#baiTable tbody tr').forEach(row => {
            row.style.display = row.textContent.toLowerCase().includes(value) ? '' : 'none';
        });
    });
}

// Đóng modal khi click ra ngoài
document.querySelectorAll('.modal').forEach(modal => {
    modal.addEventListener('click', function (e) {
        if (e.target === this) {
            this.classList.remove('active');
        }
    });
});

// Đăng xuất
function logout() {
    if (confirm('Bạn có chắc chắn muốn đăng xuất?')) {
        window.location.href = 'login.html';
    }
}

function toggleSubMenu(event, anchor) {
    event.preventDefault();
    const submenu = anchor.parentElement.querySelector('.submenu');
    if (submenu) {
        submenu.classList.toggle('open');
    }
}

function selectHocPhan(maHocPhan) {
    alert('Chọn học phần: ' + maHocPhan);
    // Xử lý hiển thị nội dung học phần bên phải
}

let baiRowToDelete = null;
document.querySelectorAll('.assignment-actions-btns .btn-danger[title="Xóa"]').forEach((btn) => {
    btn.onclick = function () {
        baiRowToDelete = btn.closest('tr');
        document.getElementById('confirmDeleteChapterModal').classList.add('active');
    }
});
function closeConfirmDeleteBai() {
    document.getElementById('confirmDeleteChapterModal').classList.remove('active');
    baiRowToDelete = null;
}

function confirmDeleteBai() {
    if (baiRowToDelete) {
        baiRowToDelete.remove();
        baiRowToDelete = null;
    }
    closeConfirmDeleteBai();
}

//function bindAssignmentTableEvents() {
//    const table = document.getElementById('assignmentTable');
//    if (!table) return;
//    table.onclick = function (e) {
//        const btn = e.target.closest('button');
//        if (!btn) return;
//        const row = btn.closest('tr');
//        if (!row) return;
//        // Chỉnh sửa
//        if (btn.classList.contains('btn-secondary') && btn.title === 'Chỉnh sửa') {
//            const name = row.children[0].innerText;
//            const desc = row.children[1].innerText;
//            const course = row.children[2].innerText;
//            const classCode = row.children[3].innerText;
//            openEditAssignmentModal(name, desc, course, classCode);
//        }
//        // Xóa
//        if (btn.classList.contains('btn-danger') && btn.title === 'Xóa') {
//            chapterRowToDelete = row;
//            document.getElementById('confirmDeleteChapterModal').classList.add('active');
//        }
//    }
//}
/*document.addEventListener('DOMContentLoaded', bindAssignmentTableEvents);*/






