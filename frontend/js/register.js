$(document).ready(function () {
    $("#registerForm").submit(function (e) {
      e.preventDefault(); // تمنع إرسال الفورم بالطريقة التقليدية
  
      const name = $("#name").val().trim();
      const email = $("#email").val().trim();
      const password = $("#password").val().trim();
  
      // تأكد من أن كل الحقول مش فاضية
      if (!name || !email || !password) {
        $("#errorMessage").text("من فضلك املأ كل الحقول.");
        return;
      }
  
      // إرسال البيانات باستخدام AJAX
      $.ajax({
        url: "https://localhost:7181/api/Logins/Register", // غيّر الرابط حسب API بتاعك
        type: "POST",
        contentType: "application/json",
        data: JSON.stringify({
          name: name,
          email: email,
          password: password
        }),
        success: function (response) {
          // مثال: لو رجع success = true
          if (response.success) {
            alert("تم التسجيل بنجاح");
            window.location.href = "login.html"; // التحويل لصفحة تسجيل الدخول
          } else {
            $("#errorMessage").text(response.message || "فشل في التسجيل.");
          }
        },
        error: function (xhr) {
          const errorResponse = xhr.responseJSON;
          $("#errorMessage").text(errorResponse?.message || "حدث خطأ أثناء التسجيل.");
        }
      });
    });
  });
  