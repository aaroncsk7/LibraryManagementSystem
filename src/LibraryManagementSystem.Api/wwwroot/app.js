// Library Management System — frontend
// Talks to the API on the same origin (relative /api/... paths), so this
// works whether you run it via `dotnet run` or from any static file server
// pointed at the API base URL.

const API = "/api";

/* ---------------------------------------------------------------- utils */

async function apiRequest(path, options = {}) {
  const res = await fetch(`${API}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...options,
  });

  if (res.status === 204) return null;

  let body = null;
  try { body = await res.json(); } catch { /* no body */ }

  if (!res.ok) {
    const message = body?.message || body?.title || `Request failed (${res.status})`;
    throw new Error(message);
  }
  return body;
}

function showToast(message, isError = false) {
  const toast = document.getElementById("toast");
  toast.textContent = message;
  toast.hidden = false;
  toast.classList.toggle("is-error", isError);
  requestAnimationFrame(() => toast.classList.add("is-visible"));
  clearTimeout(showToast._t);
  showToast._t = setTimeout(() => {
    toast.classList.remove("is-visible");
    setTimeout(() => { toast.hidden = true; }, 200);
  }, 2800);
}

function formatDate(iso) {
  if (!iso) return "—";
  const d = new Date(iso);
  return d.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
}

function escapeHtml(str) {
  return String(str ?? "").replace(/[&<>"']/g, (c) => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;",
  }[c]));
}

/* ---------------------------------------------------------------- tabs */

document.querySelectorAll(".drawer-tab").forEach((tab) => {
  tab.addEventListener("click", () => {
    document.querySelectorAll(".drawer-tab").forEach((t) => {
      t.classList.remove("is-active");
      t.setAttribute("aria-selected", "false");
    });
    tab.classList.add("is-active");
    tab.setAttribute("aria-selected", "true");

    document.querySelectorAll(".panel").forEach((p) => p.classList.remove("is-active"));
    document.getElementById(`panel-${tab.dataset.panel}`).classList.add("is-active");
  });
});

/* ---------------------------------------------------------------- clock */

function updateClock() {
  const el = document.getElementById("clock");
  el.textContent = new Date().toLocaleDateString(undefined, {
    weekday: "short", year: "numeric", month: "short", day: "numeric",
  });
}
updateClock();
setInterval(updateClock, 60_000);

/* ================================================================== BOOKS */

let books = [];
let editingBookId = null;

async function loadBooks() {
  const tbody = document.getElementById("books-table-body");
  try {
    books = await apiRequest("/books");
    renderBooks();
    renderLoanBookOptions();
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="7" class="empty-row">${escapeHtml(err.message)}</td></tr>`;
  }
}

function renderBooks() {
  const tbody = document.getElementById("books-table-body");

  if (books.length === 0) {
    tbody.innerHTML = `<tr><td colspan="7" class="empty-row">No books in the catalog yet — add the first one.</td></tr>`;
    return;
  }

  tbody.innerHTML = books.map((b) => `
    <tr>
      <td><strong>${escapeHtml(b.title)}</strong></td>
      <td>${escapeHtml(b.author)}</td>
      <td class="mono">${escapeHtml(b.isbn)}</td>
      <td>${escapeHtml(b.genre) || "—"}</td>
      <td>${b.publishedYear || "—"}</td>
      <td>
        <span class="copies-pill ${b.availableCopies > 0 ? "is-available" : "is-empty"}">
          ${b.availableCopies} / ${b.totalCopies}
        </span>
      </td>
      <td>
        <div class="row-actions">
          <button class="btn--link" data-edit-book="${b.id}">Edit</button>
          <button class="btn--link danger" data-delete-book="${b.id}">Delete</button>
        </div>
      </td>
    </tr>
  `).join("");
}

function startEditBook(id) {
  const book = books.find((b) => b.id === id);
  if (!book) return;

  editingBookId = id;
  document.getElementById("book-form-title").textContent = "Edit book";
  document.getElementById("book-id").value = book.id;
  document.getElementById("book-title").value = book.title;
  document.getElementById("book-author").value = book.author;
  document.getElementById("book-isbn").value = book.isbn;
  document.getElementById("book-genre").value = book.genre;
  document.getElementById("book-year").value = book.publishedYear;
  document.getElementById("book-copies").value = book.totalCopies;
  document.getElementById("book-submit").textContent = "Save changes";
  document.getElementById("book-cancel").hidden = false;
  document.getElementById("book-title").focus();
}

function resetBookForm() {
  editingBookId = null;
  document.getElementById("book-form").reset();
  document.getElementById("book-id").value = "";
  document.getElementById("book-form-title").textContent = "Add a book";
  document.getElementById("book-submit").textContent = "Add book";
  document.getElementById("book-cancel").hidden = true;
  document.getElementById("book-error").hidden = true;
}

document.getElementById("book-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const errorEl = document.getElementById("book-error");
  errorEl.hidden = true;

  const payload = {
    title: document.getElementById("book-title").value.trim(),
    author: document.getElementById("book-author").value.trim(),
    isbn: document.getElementById("book-isbn").value.trim(),
    genre: document.getElementById("book-genre").value.trim(),
    publishedYear: Number(document.getElementById("book-year").value),
    totalCopies: Number(document.getElementById("book-copies").value),
  };

  try {
    if (editingBookId) {
      await apiRequest(`/books/${editingBookId}`, { method: "PUT", body: JSON.stringify(payload) });
      showToast("Book updated.");
    } else {
      await apiRequest("/books", { method: "POST", body: JSON.stringify(payload) });
      showToast("Book added to the catalog.");
    }
    resetBookForm();
    await loadBooks();
  } catch (err) {
    errorEl.textContent = err.message;
    errorEl.hidden = false;
  }
});

document.getElementById("book-cancel").addEventListener("click", resetBookForm);

document.getElementById("books-table-body").addEventListener("click", async (e) => {
  const editId = e.target.dataset.editBook;
  const deleteId = e.target.dataset.deleteBook;

  if (editId) startEditBook(Number(editId));

  if (deleteId) {
    const book = books.find((b) => b.id === Number(deleteId));
    if (!confirm(`Delete "${book?.title ?? "this book"}"? This cannot be undone.`)) return;
    try {
      await apiRequest(`/books/${deleteId}`, { method: "DELETE" });
      showToast("Book deleted.");
      if (editingBookId === Number(deleteId)) resetBookForm();
      await loadBooks();
    } catch (err) {
      showToast(err.message, true);
    }
  }
});

/* ================================================================ MEMBERS */

let members = [];
let editingMemberId = null;

async function loadMembers() {
  const tbody = document.getElementById("members-table-body");
  try {
    members = await apiRequest("/members");
    renderMembers();
    renderLoanMemberOptions();
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="5" class="empty-row">${escapeHtml(err.message)}</td></tr>`;
  }
}

function renderMembers() {
  const tbody = document.getElementById("members-table-body");

  if (members.length === 0) {
    tbody.innerHTML = `<tr><td colspan="5" class="empty-row">No members yet — add the first one.</td></tr>`;
    return;
  }

  tbody.innerHTML = members.map((m) => `
    <tr>
      <td><strong>${escapeHtml(m.fullName)}</strong></td>
      <td>${escapeHtml(m.email)}</td>
      <td>${escapeHtml(m.phoneNumber) || "—"}</td>
      <td>${formatDate(m.joinDate)}</td>
      <td>
        <div class="row-actions">
          <button class="btn--link" data-edit-member="${m.id}">Edit</button>
          <button class="btn--link danger" data-delete-member="${m.id}">Delete</button>
        </div>
      </td>
    </tr>
  `).join("");
}

function startEditMember(id) {
  const member = members.find((m) => m.id === id);
  if (!member) return;

  editingMemberId = id;
  document.getElementById("member-form-title").textContent = "Edit member";
  document.getElementById("member-id").value = member.id;
  document.getElementById("member-name").value = member.fullName;
  document.getElementById("member-email").value = member.email;
  document.getElementById("member-phone").value = member.phoneNumber;
  document.getElementById("member-submit").textContent = "Save changes";
  document.getElementById("member-cancel").hidden = false;
  document.getElementById("member-name").focus();
}

function resetMemberForm() {
  editingMemberId = null;
  document.getElementById("member-form").reset();
  document.getElementById("member-id").value = "";
  document.getElementById("member-form-title").textContent = "Add a member";
  document.getElementById("member-submit").textContent = "Add member";
  document.getElementById("member-cancel").hidden = true;
  document.getElementById("member-error").hidden = true;
}

document.getElementById("member-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const errorEl = document.getElementById("member-error");
  errorEl.hidden = true;

  const payload = {
    fullName: document.getElementById("member-name").value.trim(),
    email: document.getElementById("member-email").value.trim(),
    phoneNumber: document.getElementById("member-phone").value.trim(),
  };

  try {
    if (editingMemberId) {
      await apiRequest(`/members/${editingMemberId}`, { method: "PUT", body: JSON.stringify(payload) });
      showToast("Member updated.");
    } else {
      await apiRequest("/members", { method: "POST", body: JSON.stringify(payload) });
      showToast("Member added.");
    }
    resetMemberForm();
    await loadMembers();
  } catch (err) {
    errorEl.textContent = err.message;
    errorEl.hidden = false;
  }
});

document.getElementById("member-cancel").addEventListener("click", resetMemberForm);

document.getElementById("members-table-body").addEventListener("click", async (e) => {
  const editId = e.target.dataset.editMember;
  const deleteId = e.target.dataset.deleteMember;

  if (editId) startEditMember(Number(editId));

  if (deleteId) {
    const member = members.find((m) => m.id === Number(deleteId));
    if (!confirm(`Delete "${member?.fullName ?? "this member"}"? This cannot be undone.`)) return;
    try {
      await apiRequest(`/members/${deleteId}`, { method: "DELETE" });
      showToast("Member deleted.");
      if (editingMemberId === Number(deleteId)) resetMemberForm();
      await loadMembers();
    } catch (err) {
      showToast(err.message, true);
    }
  }
});

/* ================================================================== LOANS */

let loans = [];
let loanFilter = "active";

function renderLoanBookOptions() {
  const select = document.getElementById("loan-book");
  const available = books.filter((b) => b.availableCopies > 0);
  select.innerHTML = available.length
    ? available.map((b) => `<option value="${b.id}">${escapeHtml(b.title)} (${b.availableCopies} available)</option>`).join("")
    : `<option value="" disabled selected>No copies available</option>`;
}

function renderLoanMemberOptions() {
  const select = document.getElementById("loan-member");
  select.innerHTML = members.length
    ? members.map((m) => `<option value="${m.id}">${escapeHtml(m.fullName)}</option>`).join("")
    : `<option value="" disabled selected>Add a member first</option>`;
}

async function loadLoans() {
  const tbody = document.getElementById("loans-table-body");
  try {
    const query = loanFilter === "active" ? "?active=true" : "";
    loans = await apiRequest(`/loans${query}`);
    renderLoans();
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="6" class="empty-row">${escapeHtml(err.message)}</td></tr>`;
  }
}

function loanStatusBadge(loan) {
  if (loan.isReturned) {
    return `<span class="stamp-badge is-returned">Returned ${formatDate(loan.returnDate)}</span>`;
  }
  const isOverdue = new Date(loan.dueDate) < new Date();
  const cls = isOverdue ? "stamp-badge is-overdue" : "stamp-badge";
  return `<span class="${cls}">${isOverdue ? "Overdue" : "Due"} ${formatDate(loan.dueDate)}</span>`;
}

function renderLoans() {
  const tbody = document.getElementById("loans-table-body");

  if (loans.length === 0) {
    tbody.innerHTML = `<tr><td colspan="6" class="empty-row">No ${loanFilter === "active" ? "active " : ""}loans right now.</td></tr>`;
    return;
  }

  tbody.innerHTML = loans.map((l) => `
    <tr>
      <td><strong>${escapeHtml(l.bookTitle)}</strong></td>
      <td>${escapeHtml(l.memberName)}</td>
      <td>${formatDate(l.borrowDate)}</td>
      <td>${formatDate(l.dueDate)}</td>
      <td>${loanStatusBadge(l)}</td>
      <td>
        <div class="row-actions">
          ${l.isReturned ? "" : `<button class="btn--link" data-return-loan="${l.id}">Return</button>`}
          <button class="btn--link danger" data-delete-loan="${l.id}">Delete</button>
        </div>
      </td>
    </tr>
  `).join("");
}

document.getElementById("loan-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const errorEl = document.getElementById("loan-error");
  errorEl.hidden = true;

  const bookId = Number(document.getElementById("loan-book").value);
  const memberId = Number(document.getElementById("loan-member").value);
  const dueDateInput = document.getElementById("loan-due").value;

  if (!bookId || !memberId) {
    errorEl.textContent = "Choose a book and a member first.";
    errorEl.hidden = false;
    return;
  }

  const payload = {
    bookId,
    memberId,
    dueDate: dueDateInput ? new Date(dueDateInput).toISOString() : null,
  };

  try {
    await apiRequest("/loans", { method: "POST", body: JSON.stringify(payload) });
    showToast("Book checked out.");
    document.getElementById("loan-form").reset();
    await Promise.all([loadBooks(), loadLoans()]);
  } catch (err) {
    errorEl.textContent = err.message;
    errorEl.hidden = false;
  }
});

document.getElementById("loans-table-body").addEventListener("click", async (e) => {
  const returnId = e.target.dataset.returnLoan;
  const deleteId = e.target.dataset.deleteLoan;

  if (returnId) {
    try {
      await apiRequest(`/loans/${returnId}/return`, { method: "PUT" });
      showToast("Book returned.");
      await Promise.all([loadBooks(), loadLoans()]);
    } catch (err) {
      showToast(err.message, true);
    }
  }

  if (deleteId) {
    if (!confirm("Delete this loan record? This cannot be undone.")) return;
    try {
      await apiRequest(`/loans/${deleteId}`, { method: "DELETE" });
      showToast("Loan record deleted.");
      await Promise.all([loadBooks(), loadLoans()]);
    } catch (err) {
      showToast(err.message, true);
    }
  }
});

document.querySelectorAll(".segmented__option").forEach((btn) => {
  btn.addEventListener("click", () => {
    document.querySelectorAll(".segmented__option").forEach((b) => b.classList.remove("is-active"));
    btn.classList.add("is-active");
    loanFilter = btn.dataset.filter;
    loadLoans();
  });
});

/* ---------------------------------------------------------------- init */

(async function init() {
  await Promise.all([loadBooks(), loadMembers()]);
  await loadLoans();
})();
