function generateCaptcha() {
  const characters =
    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
  let captchaText = "";

  for (let i = 0; i < 6; i++) {
    captchaText += characters.charAt(
      Math.floor(Math.random() * characters.length)
    );
  }

  return captchaText;
}

function drawCaptcha() {
  const captchaValue = generateCaptcha();
  const fonts = ["cursive"];

  let html = captchaValue
    .split("")
    .map((char) => {
      const rotate = -20 + Math.trunc(Math.random() * 30);
      const font = Math.trunc(Math.random() * fonts.length);
      return `<span
            style="
            transform:rotate(${rotate}deg);
            font-family:${font[font]};
            user-select:none;font-size:28px;
            "
           >${char} </span>`;
    })
    .join("");

  $(".captchaCanvas").html(html);

  window.captchaText = captchaValue;
}

$(document).ready(function () {
  drawCaptcha();
  $(document).on("blur", "#captchaInput", function () {
    if (!($("#captchaInput").val() == window.captchaText)) {
      $("#captchaError").text("Incorrect Captcha");
    } else $("#captchaError").text("");
  });
  $("#refreshCaptcha").click(function () {
    drawCaptcha();
  });
});
