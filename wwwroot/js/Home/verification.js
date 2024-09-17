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

  $("#verificationForm").on('submit', async function(e){
    e.preventDefault();
    const otp= $("#otp").val();
    const backupCode = $("#backupCode").val();
    const formdata = new FormData();
    formdata.append("otp",otp);
    formdata.append("backupCode",backupCode);

    const response = await fetch("/Home/Verification",{method:'post',body:formdata});
    const result = await response.json();
    console.log(result);
    if(result.status) {
      let url = result.userType == "Citizen"?"/User/Index":result.userType=="Officer"?"/Officer/Index":"/Admin/Dashboard";
      window.location.href = url;
    }
    else if(result.url) window.location.href = url;
    else $(this).parent().append(`<br/><p class="text-danger text-center">${result.message}</p>`)
  })
});
