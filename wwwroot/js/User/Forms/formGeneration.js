function generateServiceForm(formData) {
  appendFormFields($("#form1 .row"), formData[0].fields, "col-sm-6", 0);
  appendFormFields($("#form2 .row"), formData[1].fields, "col-sm-4", 1);
  $("#form2 .row").append(
    `<hr style="border:none;height:5px;background-color:#000;">`
  );
  appendFormFields($("#form2 .row"), formData[2].fields, "col-sm-4", 2);
  appendFormFields($("#form3 .row"), formData[3].fields, "col-sm-12", 3);
  appendFormFields($("#form4 .row"), formData[4].fields, "col-sm-6", 4);

  getDistricts().then((options) => {
    options.forEach((option) => {
      $("[id*='district'], [id*='District']").append(option);
    });
    $("[id*='district'], [id*='District']").each(function () {
      const isOldValue = $(this).attr("value");
      const id = $(this).attr("id");
      if (isOldValue) {
        if (/^\d+$/.test(isOldValue)) {
          $(`#${id} option[value="${isOldValue}"]`)
            .prop("selected", true)
            .trigger("change");
        }
      }
    });
  });
}

// Create Input
function createInput(obj, columSize, formNo) {
  const validationFunctions =
    obj.validationFunctions?.map((item) => validationFunctionsList[item]) || [];
  if (validationFunctions.length) {
    attachValidation(obj, validationFunctions);
  }

  let details = null;
  let key;
  let readonly = true;
  if (ApplicationId != null) {
    const editList = JSON.parse(application.generalDetails.editList);
    if (editList != undefined) {
      if (editList.includes(obj.name)) readonly = false;
    } else readonly = false;

    if (formNo === 0) {
      details = {
        ...application.generalDetails,
        ...JSON.parse(application.generalDetails.serviceSpecific),
      };
      key = obj.isFormSpecific
        ? obj.name
        : obj.name.charAt(0).toLowerCase() + obj.name.slice(1);
    } else if (formNo === 1 && PresentAddressId != null) {
      details = application.preAddressDetails[0];
      key =
        obj.name.replace("Present", "").charAt(0).toLowerCase() +
        obj.name.replace("Present", "").slice(1);
    } else if (formNo === 2 && PermanentAddressId != null) {
      details = application.perAddressDetails[0];
      if (details.addressId === application.preAddressDetails[0].addressId) {
        $("#SameAsPresent").prop("checked", true);
      }
      key =
        obj.name.replace("Permanent", "").charAt(0).toLowerCase() +
        obj.name.replace("Permanent", "").slice(1);
    } else if (formNo == 3 && application.generalDetails.bankDetails != "") {
      details = JSON.parse(application.generalDetails.bankDetails);
      key = obj.name;
    } else if (formNo == 4 && application.generalDetails.documents != "") {
      const documents = JSON.parse(application.generalDetails.documents);
      details = {};
      documents.map((item) => {
        details[item.Label + "Enclosure"] = item.Enclosure;
        details[item.Label + "File"] = item.File;
      });
      key = obj.name;
    }
  } else {
    readonly = false;
  }

  const label = `<label for="${obj.name}">${obj.label}</label>`;
  let value = details ? details[key] || "" : "";

  if (details != null) {
    if (formNo == 0) {
      generalForm.append(obj.name, value);
    } else if (formNo == 1) {
      if (
        obj.name.includes("District") ||
        obj.name.includes("Tehsil") ||
        obj.name.includes("Block")
      ) {
        if (details[key + "Id"]) value = details[key + "Id"];
      }
      addressForm.append(obj.name, value);
    } else if (formNo == 2) {
      if (
        obj.name.includes("District") ||
        obj.name.includes("Tehsil") ||
        obj.name.includes("Block")
      ) {
        if (details[key + "Id"]) value = details[key + "Id"];
      }
      addressForm.append(obj.name, value);
    } else if (formNo == 3) {
      bankForm.append(obj.name, value);
    } else if (formNo == 4) {
      documentForm.append(obj.name, value);
    }
  }

  if (obj.type === "select") {
    const options = obj.options || [];

    const selectOptions = options
      .map((item) => `<option value="${item}">${item}</option>`)
      .join("");

    if ((formNo = 1) && ApplicationId != null && details != null) {
      if (
        obj.name.includes("District") ||
        obj.name.includes("Tehsil") ||
        obj.name.includes("Block")
      ) {
        if (details[key + "Id"]) value = details[key + "Id"];
      }
    }

    return `
        <div class="${columSize} mb-2">
            ${label}
            <select class="form-select" name="${obj.name}" id="${
      obj.name
    }" value="${value}" ${readonly && "disabled"}>
                ${selectOptions}
            </select>
        </div>
    `;
  } else if (obj.type == "radio") {
    const radioOptions = obj.options
      .map(
        (item) =>
          `<input type="radio" class="form-check-input ${obj.name}" name="${obj.name}" value="${item}"><span class="px-1 pe-4">${item}</span>`
      )
      .join("");
    return `
        <div class="${columSize} mb-2">
          ${radioOptions}
          <input type="text" class="form-control" id="${obj.label}" name="${obj.label}" />
        </div>
    `;
  } else {
    const classType = obj.type === "checkbox" ? "form-check" : "form-control";
    const maxLength = obj.maxLength || 100;
    const accept = obj.type === "file" ? obj.accept : null; // Accept only relevant for file inputs

    // Conditionally setting the image source for a specific application context
    if (ApplicationId != null && obj.name === "ApplicantImage") {
      $("#profile").attr("src", value);
    }

    // Using a common template for the input, conditionally modified for file type
    const inputType =
      obj.type === "file" && ApplicationId != null && details != null
        ? "text"
        : obj.type; // Use 'text' type for file to make it readonly visibly
    const extraClasses =
      obj.type === "file" && details != null ? " file-input" : "";
    const readOnlyAttribute = readonly ? " readonly" : "";
    const acceptAttribute = obj.type === "file" ? ` accept="${accept}"` : ""; // Only add accept for file types

    const inputTemplate = `
    <div class="${columSize} mb-2">
        ${label}
        <input class="${classType}${extraClasses}" type="${inputType}" name="${obj.name}" 
               id="${obj.name}" placeholder="${obj.label}" maxlength="${maxLength}"${acceptAttribute}
               value="${value}"${readOnlyAttribute}>
    </div>
`;

    return inputTemplate;
  }
}

function appendFormFields(container, fields, columSize, formNo) {
  if (formNo == 1)
    container.append(
      `<p class="text-center fw-bold fs-3">Present Address Details</p>`
    );
  else if (formNo == 2)
    container.append(
      `<p class="text-center fw-bold fs-3">Permanent Address Details</p>`
    );
  fields.forEach((item) => {
    container.append(createInput(item, columSize, formNo));
  });
}
