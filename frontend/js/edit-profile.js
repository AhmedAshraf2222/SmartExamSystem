$(document).ready(function () {
  // تحميل بيانات الطبيب عند فتح الصفحة
  loadCurrentDoctor();

  // معالجة إرسال النموذج
  $("#editProfileForm").submit(function (e) {
    e.preventDefault();
    updateDoctorProfile();
  });

  // زر إظهار/إخفاء كلمة المرور
  $("#togglePassword").on("click", function () {
    const passwordInput = $("#password");
    const type =
      passwordInput.attr("type") === "password" ? "text" : "password";
    passwordInput.attr("type", type);
    $(this).text(type === "password" ? "👁️" : "🙈");
  });
});

function loadCurrentDoctor() {
  const token = localStorage.getItem("token");
  const doctorId = localStorage.getItem("doctorId");

  if (!token || !doctorId) {
    window.location.href = "login.html";
    return;
  }

  $.ajax({
    url: `https://localhost:7181/api/Logins/Doctors/${doctorId}`,
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
    },
    success: function (response) {
      $("#name").val(response.name);
      $("#email").val(response.email);
    },
    error: function (xhr) {
      if (xhr.status === 401) {
        alert("Session expired. Please login again.");
        window.location.href = "login.html";
      } else {
        $("#errorMessage").text(
          "Failed to load profile: " +
            (xhr.responseJSON?.message || "Server error")
        );
      }
    },
  });
}

function updateDoctorProfile() {
  const token = localStorage.getItem("token");
  const doctorId = localStorage.getItem("doctorId");

  if (!token || !doctorId) {
    window.location.href = "login.html";
    return;
  }

  const name = $("#name").val().trim();
  const email = $("#email").val().trim();
  const password = $("#password").val().trim();

  // تحقق من البيانات المطلوبة
  if (!name || !email) {
    $("#errorMessage").text("Name and email are required.");
    return;
  }

  // تحقق من الباسورد لو اتكتب
  if (password && password.length < 6) {
    $("#errorMessage").text("Password must be at least 6 characters.");
    return;
  }

  const updateData = { name, email };
  if (password) {
    updateData.password = password;
  }

  $.ajax({
    url: `https://localhost:7181/api/Logins/Doctors/${doctorId}`,
    method: "PUT",
    contentType: "application/json",
    headers: {
      Authorization: `Bearer ${token}`,
    },
    data: JSON.stringify(updateData),
    success: function (response) {
      if (response.success) {
        alert("Profile updated successfully!");
        if (password && response.token) {
          localStorage.setItem("token", response.token);
        }
        window.location.href = "index.html";
      } else {
        $("#errorMessage").text(response.message || "Update failed.");
      }
    },
    error: function (xhr) {
      if (xhr.status === 401) {
        alert("Session expired. Please login again.");
        window.location.href = "login.html";
      } else {
        const errorMsg =
          xhr.responseJSON?.message ||
          "An error occurred while updating profile.";
        $("#errorMessage").text(errorMsg);
      }
    },
  });
}
