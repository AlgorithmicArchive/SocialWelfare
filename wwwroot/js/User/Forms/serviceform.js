$(document).ready(function () {
  $("form").prop("autocomplete", "off");
  const formData = JSON.parse(serviceContent.formElement);
  generateServiceForm(formData);
  $("#serviceName").text(serviceContent.serviceName);
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

  $(document).on("click", ".file-input", function () {
    if (!$(this).prop("readonly"))
      $(this).attr("type", "file").trigger("click");
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
      $("html, body").animate({ scrollTop: 0 }, "slow");
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
    $(`#form${stepCount} input`).blur();
    if ($(`#form${stepCount} .errorMsg`).length === 0) {
      handleFormAppend(stepCount);
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

  $('input[type="radio"]:first').prop("checked", true);

  $("#RelationName").attr(
    "placeholder",
    $(".Relation:checked").val() + " Name"
  );

  $(document).on("change", ".Relation", function () {
    console.log("first");
    $("#RelationName").attr(
      "placeholder",
      $(".Relation:checked").val() + " Name"
    );
  });

  $("[id*='Present']").on("blur", function () {
    if ($("#SameAsPresent").prop("checked")) {
      copyPresentToPermanent();
    }
  });

  $(document).on("focus", "input[name*=Date]", function () {
    if ($(this).attr("type") == "text") {
      var currentYear = new Date().getFullYear();
      $(this).prop("readonly", true); // Make the input readonly
      $(this).datepicker({
        dateFormat: "dd/M/yy",
        changeMonth: true,
        changeYear: true,
        yearRange: "1990:" + currentYear,
        onSelect: function (dateText) {
          $(this).val(dateText);
          $(this).blur();
        },
      });
    }
  });

  handleDistrictChange("#PresentDistrict", "#PresentTehsil", "#PresentBlock");
  handleDistrictChange(
    "#PermanentDistrict",
    "#PermanentTehsil",
    "#PermanentBlock"
  );
});
