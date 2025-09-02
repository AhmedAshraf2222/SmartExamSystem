$(document).ready(function () {
  // تشغيل إظهار/إخفاء كلمة المرور
  $("#togglePassword").on("click", function () {
    const passwordInput = $("#password");
    const currentType = passwordInput.attr("type");
    passwordInput.attr(
      "type",
      currentType === "password" ? "text" : "password"
    );
    $(this).text(currentType === "password" ? "🙈" : "👁️");
  });

  // معالجة تسجيل الدخول
  $("#loginForm").on("submit", function (e) {
    e.preventDefault(); // منع الإرسال الافتراضي

    const email = $("#email").val();
    const password = $("#password").val();

    $.ajax({
      url: "https://localhost:7181/api/Logins/Login",
      method: "POST",
      contentType: "application/json",
      data: JSON.stringify({
        email: email,
        password: password,
      }),
      success: function (response) {
        if (response.success) {
          localStorage.setItem("token", response.token);
          localStorage.setItem("doctorId", response.doctor.doctorId);
          window.location.href = "index.html";
        } else {
          alert("Login failed: " + response.message);
        }
      },
      error: function (xhr, status, error) {
        console.error("Error:", error);
        alert("فشل الاتصال بالخادم أو بيانات غير صحيحة");
      },
    });
  });
});
