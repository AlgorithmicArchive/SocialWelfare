$(document).ready(function () {
  $("form").prop("autocomplete", "off");
  $("#serviceName").text(application.serviceContent.serviceName);

  const formData = JSON.parse(serviceContent.formElement);
  generateServiceForm(formData);

  let stepCount = 1;

  $("#previous").attr("disabled", stepCount === 1);

  $("#dummyData").click(function () {
    appendDummyData(stepCount);
  });

  $("#ApplicantImage").change((event) => {
    const reader = new FileReader();
    reader.onload = () => {
      $("#profile").attr("src", reader.result);
    };
    reader.readAsDataURL(event.target.files[0]);
  });

  $("#next").click(() => {
    $(`#form${stepCount} input`).blur();
    if ($(`#form${stepCount} .errorMsg`).length === 0) {
      if (stepCount < 4) {
        handleFormAppend(stepCount);
        $(`#form${stepCount}`).hide();
        $(`#form${stepCount + 1}`).show();
        oneStepComplete();
        $("#previous").attr("disabled", false);
        stepCount++;
        if (stepCount === 4) {
          $("#next").attr("disabled", true);
          $("#submit").show();
        }
      }
    }
  });

  $("#previous").click(() => {
    if (stepCount > 1) {
      $(`#form${stepCount}`).hide();
      $(`#form${stepCount - 1}`).show();
      $("#next").attr("disabled", false);
      stepCount--;
      $("#previous").attr("disabled", stepCount === 1);
      if (stepCount < 4) {
        $("#submit").hide();
      }
    }
  });

  $("#submit").click(() => {
    console.log(ApplicationId);
    $(`#form${stepCount} input`).blur();
    if ($(`#form${stepCount} .errorMsg`).length === 0) {
      handleFormAppend(stepCount);
      console.log("POP UP.");
    }
  });

  $("#SameAsPresent").change(function () {
    if ($(this).prop("checked")) {
      $(this).val(true);
      copyPresentToPermanent();
    } else {
      $(this).val(false);
      empytPermanent();
    }
  });

  $("[id*='Present']").on("blur", function () {
    if ($("#SameAsPresent").prop("checked")) {
      copyPresentToPermanent();
    }
  });

  handleDistrictChange("#PresentDistrict", "#PresentTehsil", "#PresentBlock");
  handleDistrictChange(
    "#PermanentDistrict",
    "#PermanentTehsil",
    "#PermanentBlock"
  );
});
