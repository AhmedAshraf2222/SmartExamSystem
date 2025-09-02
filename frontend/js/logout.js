$(document).ready(function () {
  $("#logoutBtn").on("click", function (e) {
    e.preventDefault();
    // Clear localStorage
    localStorage.removeItem("token");
    localStorage.removeItem("doctorId");
    // Redirect to login page
    window.location.href = "login.html";
  });
});
