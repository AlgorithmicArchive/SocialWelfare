const formUI = {
  appendForm: function (containerId, formType) {
    const formTitle = formType === "login" ? "Login" : "Register";
    const buttonLabel = formType === "login" ? "Login" : "Register";
    const additionalFields =
      formType === "register"
        ? `
      <label for="Email">Email</label>
      <input type="email" name="Email" id="Email" placeholder="Email"
          class="form-control border-0 border-bottom rounded-0 mb-2">
       <label for="MobileNumber">MobileNumber</label>
      <input type="text" name="MobileNumber" id="MobileNumber" placeholder="Mobile Number"
          class="form-control border-0 border-bottom rounded-0 mb-2">
           <label for="Password">Password</label>
          <input type="password" name="Password" id="Password" placeholder="Password"
              class="form-control border-0 border-bottom rounded-0 mb-2">
      <label for="Confirm Password">Confirm Password</label>
      <input type="password" name="ConfirmPassword" id="ConfirmPassword"
          placeholder="Confirm Password" class="form-control border-0 border-bottom rounded-0 mb-2">
    `
        : `
           <label for="Password">Password</label>
          <input type="password" name="Password" id="Password" placeholder="Password"
              class="form-control border-0 border-bottom rounded-0 mb-2">
        `;

    $(`#${containerId}`).append(`
      <p class="form-heading text-center">${formTitle}</p>
      <div class="container d-flex flex-column">
          <label for="Username">Username</label>
          <input type="text" name="Username" id="Username" placeholder="Username"
              class="form-control border-0 border-bottom rounded-0 mb-2">
          ${additionalFields}
           <div class="d-flex justify-content-between align-items-center px-3 border rounded-pill mb-2">
           <span id="captchaCanvas" class="captchaCanvas d-flex justify-content-center align-items-center w-75 p-0"></span>
           <i id="refreshCaptcha" class="fa-solid fa-rotate fs-4"></i>
          </div>
          <label for="Captcha">Captcha</label>
          <input type="text" class="form-control border-0 border-bottom rounded-0 mb-2" id="captchaInput" placeholder="Captcha">
          <p id="captchaError" class="fs-6 text-danger text-center"></p>
          <button class="btn btn-dark w-25 mx-auto">${buttonLabel}</button>
      </div>
      <p class="fs-5 text-center mt-4">OR</p>
      <p id="switchTo${
        formType === "login" ? "Register" : "Login"
      }" class="text-center" role="button">${
      formType === "login" ? "Register" : "Login"
    }</p>
    `);
  },
  switchForm: function (screenWidth, formType) {
    const oppositeFormType = formType === "login" ? "register" : "login";
    const formHeadingText =
      formType === "login" ? "Welcome Back" : "Welcome, Please Sign Up";
    const formImageUrl =
      formType === "login" ? "/resources/login.png" : "/resources/register.png";

    if (screenWidth < 768) {
      $(`#${oppositeFormType}Container`).hide();
      $(`#${formType}Container`).show();
    } else {
      $("#formOverlay")
        .removeClass(formType === "login" ? "right-to-left" : "left-to-right")
        .addClass(formType === "login" ? "left-to-right" : "right-to-left");
      $("#formHeading").text(formHeadingText);
      $("#formImage").attr("src", formImageUrl);
    }

    $(`#${oppositeFormType}Container`).empty();
    this.appendForm(`${formType}Container`, formType);
    drawCaptcha();
  },
};

$(document).ready(function () {
  console.log(window.captchaText, $("#captchaInput").val());
  $("#registerContainer").empty();

  $("form").attr("autocomplete", "off");

  const screenWidth = $(window).width();
  if (screenWidth < 768) {
    $("#registerContainer").hide();
  }

  $(document).on("click", "#switchToLogin", function () {
    formUI.switchForm(screenWidth, "login");
  });

  $(document).on("click", "#switchToRegister", function () {
    formUI.switchForm(screenWidth, "register");
  });

  // Login/Registration Logic
  $("#authForm").on("submit", function (e) {
    e.preventDefault();
    $("#captchaInput").trigger("blur");
    const formdata = new FormData(this);
    showSpinner();
    $("#registerButton").append(`
     <div id="loadingSpinner" class="spinner-border text-muted"></div>
    `);

    let errorPersists = false;
    $(".errorMsg").each(function () {
      if ($(this).length) {
        errorPersists = true;
      }
    });

    // if (window.captchaText != $("#captchaInput").val()) {
    //   errorPersists = true;
    // }

    if (!errorPersists) {
      let formType = "";
      if (!formdata.has("Email")) formType = "login";
      else formType = "register";

      formdata.append("formType", formType);

      fetch("/Home/Authentication", { method: "post", body: formdata })
        .then((res) => res.json())
        .then((data) => {
          hideSpinner();
          if (formType == "register") {
            $("#loadingSpinner").remove();
            if (data.status) {
              console.log(data);
              $("#otpButton").click();
              $("#otpForm").append(
                `<input type="hidden" id="CitizenId" name="CitizenId" value="${data.citizenId}"/>`
              );
            } else {
              $("#registerButton").after(
                `<p class="fs-6 text-danger text-center p-2">${data.response}</p>`
              );
            }
          } else {
            if (data.status) {
              console.log(data.url);
              window.location.href = data.url;
            } else {
              $("#loginButton").after(
                `<p class="fs-6 text-danger text-center p-2">${data.response}</p>`
              );
            }
          }
        })
        .catch((err) => {
          console.log(err);
        });
    } else {
      hideSpinner();
    }
  });

  $("#otpForm").on("submit", function (e) {
    e.preventDefault();
    const formdata = new FormData(this);
    console.log(formdata);
    fetch("/Home/OTPValidation", { method: "post", body: formdata })
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          $("#otpButton").click();
          $("#registerButton").after(
            `<p class="fs-6 text-success text-center p-2">${data.response}</p>`
          );
        } else {
          $("#otpButton").click();
          $("#registerButton").after(
            `<p class="fs-6 text-success text-center p-2">${data.response}</p>`
          );
        }
      });
  });

  $(document).on("blur input", "input", async function () {
    if ($("#Email").length) {
      const id = $(this).attr("id");
      const value = $(this).val();
      let errorList = [];
      if (id == "Username") {
        errorList = await validateUsername(value);
      } else if (id == "Password") errorList = validatePassword(value);
      else if (id == "Email") {
        errorList = await validateEmail(value);
      } else if (id == "MobileNumber") {
        errorList = await validateMobileNumber(value);
      } else if (id == "ConfirmPassword" && value != $("#Password").val())
        errorList = ["Confirm Password should be same as Password."];

      AddErrorSpan(id, errorList);
    }
  });
});
