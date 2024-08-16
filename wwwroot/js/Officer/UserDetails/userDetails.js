$(document).ready(function () {
  const generalDetails = ApplicationDetails.generalDetails;
  const preAddressDetails = ApplicationDetails.preAddressDetails;
  const perAddressDetails = ApplicationDetails.perAddressDetails;
  const bankDetails = JSON.parse(generalDetails.bankDetails);
  const documents = JSON.parse(generalDetails.documents);
  const previousActions = ApplicationDetails.previousActions;
  const canOfficerTakeAction = ApplicationDetails.canOfficerTakeAction;
  const letterUpdateDetails = JSON.parse(
    ApplicationDetails.serviceContent.letterUpdateDetails
  );

  getWorkForceOfficer(
    ApplicationDetails.serviceContent,
    ApplicationDetails.currentOfficer,
    ApplicationDetails.generalDetails.applicationId,
    letterUpdateDetails
  );

  if ($("#action").val() == "Sanction") {
    CertificateDetails(letterUpdateDetails, generalDetails);
  }

  if (
    $("#action").val() == "Forward" &&
    ApplicationDetails.currentOfficer == "District Social Welfare Officer" &&
    updateColumn != null
  ) {
    $("#extra").empty();
    $("#extra").append(
      `
        <label title="${updateColumn.label} Certificate by TSWO">${
        updateColumn.label
      } Certificate by TSWO</label>
        <input class="form-control" type="file" name="${ApplicationDetails.currentOfficer
          .split(" ")
          .join("_")}_Document" id="${ApplicationDetails.currentOfficer
        .split(" ")
        .join("_")}_Document" required>
    `
    );
  }

  const excludedProperties = [
    "phase",
    "bankDetails",
    "documents",
    "applicationStatus",
    "applicantImage",
    "updateRequest",
    "editList",
    "applicationsHistories",
    "service",
  ];

  // Set user image
  $("#userImage").attr("src", generalDetails.applicantImage);

  // Append details to respective sections
  appendGeneralDetails(generalDetails, excludedProperties);
  appendDetails("addressDetails", preAddressDetails, "Present ");
  $("#addressDetails").append(`<hr>`);
  appendDetails("addressDetails", perAddressDetails, "Permanent ");
  appendBankDetails(bankDetails);
  appendDocuments(documents);
  appendPerviousActions(previousActions);

  if (!canOfficerTakeAction) {
    $("#takeAction").after(
      `<p class="text-danger width-50 mt-5">Cannot proceed with this application because it has been either more than 15 days since it was received by you or more than 45 days since it was submitted.</p>`
    );
    $("#takeAction").remove();
  }

  $("#action").on("change", function () {
    const value = $(this).val();
    $("#extra").empty().removeClass("border border-dark p-3");
    $("#ceritificateDetails").hide();
    if (value == "ReturnToEdit") {
      const formElements = JSON.parse(
        ApplicationDetails.serviceContent.formElement
      );
      const addedLabels = new Set();

      $("#extra").addClass("border border-dark p-3");

      formElements.forEach((item) => {
        $("#extra").append(`<p class="fs-5 fw-bold">${item.section}</p>`);
        const ul = $(`<ul class="list-unstyled" />`);
        item.fields.forEach((each) => {
          if (
            !addedLabels.has(each.label) &&
            each.name != "District" &&
            each.name != "SameAsPresent"
          ) {
            addedLabels.add(each.label);
            $(ul).append(`
                <li class="d-flex gap-2"><input type="checkbox" class="form-check editColumn" value="${each.name}" />${each.label}</li>
          `);
          }
        });
        $("#extra").append(ul);
        $("#extra").append(`<hr>`);
      });
    } else if (value == "Update") {
      $("#extra").append(
        `<label>${updateColumn.label} to be updated</label><input type="${
          updateColumn.type == "date" ? "text" : updateColumn.type
        }" class="form-control datepicker-input" id="${
          updateColumn.name
        }" name="${updateColumn.name}" value="${updateColumn.value}"/>
        <label class="mt-1">Document of proof</label>
        <input type="file" id="${updateColumn.name}File" class="form-control"/>
        `
      );

      const validationFunctions =
        updateColumn.validationFunctions?.map(
          (item) => validationFunctionsList[item]
        ) || [];
      if (validationFunctions.length) {
        attachValidation(updateColumn, validationFunctions);
      }
    } else if (value == "Forward") {
      $("#extra").empty();
      $("#extra").append(`
          <label title="${updateColumn.label} Certificate by TSWO">${
        updateColumn.label
      } Certificate by TSWO</label>
          <input class="form-control" type="file" name="${ApplicationDetails.currentOfficer
            .split(" ")
            .join("_")}_Document" id="${ApplicationDetails.currentOfficer
        .split(" ")
        .join("_")}_Document" />
      `);
    } else if (value == "Sanction") {
      CertificateDetails(letterUpdateDetails, generalDetails);
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
  $(document).on("change", ".editColumn", function () {
    const value = $(this).val();
    if (value.toLowerCase().includes("district")) {
      $(this)
        .parent()
        .parent()
        .find('input[value*="Tehsil"],input[value*="Block"]')
        .attr("checked", true);
    }
  });
});
