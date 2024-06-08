$(document).ready(function () {
  $("#otpButton").click(function () {
    showSpinner();
    fetch("/Home/SendOtp", { method: "get" })
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          hideSpinner();
          $("#mainContainer").hide();
          $("#otpContainer").show();
        }
      });
  });

  $("#backupButton").click(function () {
    $("#mainContainer").hide();
    $("#backupContainer").show();
  });
});
