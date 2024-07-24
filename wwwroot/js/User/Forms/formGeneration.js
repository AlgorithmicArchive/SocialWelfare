function generateServiceForm(formData) {
  appendFormFields($("#form1 .row"), formData[0].fields, "col-sm-6", 0);
  appendFormFields($("#form2 .row"), formData[1].fields, "col-sm-12", 1);
  appendFormFields($("#form2 .row"), formData[2].fields, "col-sm-12", 2);
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
  attachValidations(obj);
  const { details, key, readonly } = determineDetailsAndReadonlyStatus(
    obj,
    formNo
  );
  const label = createLabel(obj);
  const value = getValue(details, key);
  appendFormData(formNo, obj, details, key, value);

  switch (obj.type) {
    case "select":
      return createSelectInput(obj, columSize, label, value, readonly);
    case "radio":
      return createRadioInput(obj, columSize, label, value, readonly);
    default:
      return createOtherInputTypes(
        formNo,
        obj,
        details,
        columSize,
        label,
        value,
        readonly
      );
  }
}

function attachValidations(obj) {
  const validationFunctions =
    obj.validationFunctions?.map((item) => validationFunctionsList[item]) || [];
  if (validationFunctions.length) {
    attachValidation(obj, validationFunctions);
  }
}

function determineDetailsAndReadonlyStatus(obj, formNo) {
  let details = null;
  let key;
  let readonly = true;

  if (ApplicationId != null) {
    const editList = JSON.parse(application.generalDetails.editList || "[]");
    readonly = !editList.includes(obj.name);

    switch (formNo) {
      case 0:
        details = {
          ...application.generalDetails,
          ...JSON.parse(application.generalDetails.serviceSpecific || "{}"),
        };
        key = obj.isFormSpecific
          ? obj.name
          : obj.type == "radio"
          ? obj.name.charAt(0).toLowerCase() + obj.name.slice(1) + "Name"
          : obj.name.charAt(0).toLowerCase() + obj.name.slice(1);
        break;
      case 1:
        if (PresentAddressId != null) {
          details = application.preAddressDetails[0];
          key =
            obj.name.replace("Present", "").charAt(0).toLowerCase() +
            obj.name.replace("Present", "").slice(1);
        }
        break;
      case 2:
        if (PermanentAddressId != null) {
          details = application.perAddressDetails[0];
          if (
            details.addressId === application.preAddressDetails[0].addressId
          ) {
            $("#SameAsPresent").prop("checked", true);
          }
          key =
            obj.name.replace("Permanent", "").charAt(0).toLowerCase() +
            obj.name.replace("Permanent", "").slice(1);
        }
        break;
      case 3:
        if (application.generalDetails.bankDetails) {
          details = JSON.parse(application.generalDetails.bankDetails);
          key = obj.name;
        }
        break;
      case 4:
        if (application.generalDetails.documents) {
          const documents = JSON.parse(application.generalDetails.documents);
          details = {};
          documents.forEach((item) => {
            details[item.Label + "Enclosure"] = item.Enclosure;
            details[item.Label + "File"] = item.File;
          });
          key = obj.name;
        }
        break;
      default:
        readonly = false;
    }
  } else {
    readonly = false;
  }

  return { details, key, readonly };
}

function createLabel(obj) {
  return `<label for="${obj.name}" title="${obj.label}">${obj.label}</label>`;
}

function getValue(details, key) {
  return details ? details[key] || "" : "";
}

function appendFormData(formNo, obj, details, key, value) {
  if (details != null) {
    const appendForm =
      formNo === 0
        ? generalForm
        : [1, 2].includes(formNo)
        ? addressForm
        : formNo === 3
        ? bankForm
        : documentForm;

    if (
      [1, 2].includes(formNo) &&
      obj.name.match(/(District|Tehsil|Block)/) &&
      details[key + "Id"]
    ) {
      value = details[key + "Id"];
    }

    appendForm.append(obj.name, value);
  }
}

function createSelectInput(obj, columSize, label, value, readonly) {
  const options = obj.options || [];
  const selectOptions = options
    .map((item) => `<option value="${item}">${item}</option>`)
    .join("");
  return `
    <div class="${columSize} mb-2">
      ${label}
      <select class="form-select" name="${obj.name}" id="${obj.name}" ${
    readonly ? "disabled" : ""
  } value="${value}">
        ${selectOptions}
      </select>
    </div>
  `;
}

function createRadioInput(obj, columSize, label, value, readonly) {
  const radioOptions = obj.options
    .map(
      (item) => `
      <input type="radio" class="form-check-input ${obj.name}" name="${
        obj.name
      }" value="${item}" ${readonly ? "disabled" : ""}>
      <span class="px-1 pe-4">${item}</span>
    `
    )
    .join("");

  return `
    <div class="${columSize} mb-2">
      ${radioOptions}
      <input type="text" class="form-control" id="${obj.label}" name="${obj.label}" value="${value}"/>
    </div>
  `;
}

function createOtherInputTypes(
  formNo,
  obj,
  details,
  columSize,
  label,
  value,
  readonly
) {
  const classType = obj.type === "checkbox" ? "form-check" : "form-control";
  const maxLength = obj.maxLength || 100;
  const accept = obj.type === "file" ? obj.accept : null;

  if (ApplicationId != null && obj.name === "ApplicantImage") {
    $("#profile").attr("src", value);
  }

  const inputType =
    obj.type === "file" && ApplicationId != null && details != null
      ? "text"
      : obj.type == "date"
      ? "text"
      : obj.type;
  const extraClasses =
    obj.type === "file" && details != null ? " file-input" : "";
  const readOnlyAttribute = readonly ? " readonly" : "";
  const acceptAttribute = obj.type === "file" ? ` accept="${accept}"` : "";

  return `
    <div class="${columSize} mb-2">
      ${formNo == 4 && obj.type == "file" ? "<label></label>" : label}
      <input class="${classType}${extraClasses}" type="${inputType}" name="${
    obj.name
  }" id="${obj.name}"
        placeholder="${obj.type == "date" ? "dd/mm/yyyy" : obj.label}"
        maxlength="${maxLength}"${acceptAttribute} value="${value}" ${readOnlyAttribute} ${
    PresentAddressId != null && PresentAddressId == PermanentAddressId
      ? "checked"
      : ""
  }>
    </div>
  `;
}

function appendFormFields(container, fields, columSize, formNo) {
  if ([1, 2].includes(formNo)) {
    const columnTitle =
      formNo === 1 ? "Present Address Details" : "Permanent Address Details";
    const column = $(`
      <div class="col-md-6">
        <p class="text-center fw-bold fs-3">${columnTitle}</p>
        ${
          formNo === 1
            ? `<div class="col-sm-12" style="height:55px;"></div>`
            : ""
        }
      </div>
    `);
    fields.forEach((item) => {
      column.append(createInput(item, columSize, formNo));
    });
    container.append(column);
  } else if (formNo === 4) {
    const totalDocs = fields.length;
    const column1 = $(
      `<div class="col-md-6 border-end border-dark"><div class="row"></div></div>`
    );
    const column2 = $(`<div class="col-md-6"><div class="row"></div></div>`);
    fields.forEach((item, index) => {
      if (index < totalDocs / 2) {
        column1.find(".row").append(createInput(item, columSize, formNo));
      } else {
        column2.find(".row").append(createInput(item, columSize, formNo));
      }
    });
    container.append(column1).append(column2);
  } else {
    fields.forEach((item) => {
      container.append(createInput(item, columSize, formNo));
    });
  }
}
