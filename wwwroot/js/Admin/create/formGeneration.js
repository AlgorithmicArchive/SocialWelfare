function GetDistricts() {
  fetch("/Admin/GetDistricts")
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        let list = ``;
        data.districts.map((item) => {
          list += `<option value="${item.uuid}">${item.districtName}</option>`;
        });

        $(`select[name*="District"`).each(function () {
          $(this).append(list);
          $(this).attr("value", "1");
          $(this).trigger("change");
        });
      }
    });
}
function GetTehsil(value) {
  fetch("/Admin/GetTeshilForDistrict?districtId=" + value)
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        let list = ``;
        data.tehsils.map((item) => {
          list += `<option value="${item.uuid}">${item.tehsilName}</option>`;
        });

        $(`select[name*="Tehsil"`).each(function () {
          $(this).empty();
          $(this).append(list);
        });
      }
    });
}
function GetBlock(value) {
  fetch("/Admin/GetBlockForDistrict?districtId=" + value)
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        let list = ``;
        data.blocks.map((item) => {
          list += `<option value="${item.uuid}">${item.blockName}</option>`;
        });

        $(`select[name*="Block"`).each(function () {
          $(this).append(list);
        });
      }
    });
}
function convertToTitleCase(input) {
  // Replace camelCase with spaces
  let stringWithSpaces = input.replace(/([a-z])([A-Z])/g, "$1 $2");
  // Capitalize the first letter of each word
  return stringWithSpaces.charAt(0).toUpperCase() + stringWithSpaces.slice(1);
}
function CreateInputField(element) {
  const Class = element.type == "checkbox" ? "form-check" : "form-control";
  return `<input type="${element.type}" class="${Class} mb-2" name="${element.name}" id="${element.name}" placeholder="${element.label}" maxlength=${element.maxLength} />`;
}
function CreateRadioField(element) {
  const radioOptions = element.options
    .map(
      (item) =>
        `<input type="radio" class="form-check-input ${element.name}" name="${element.name}" value="${item}"><span class="px-1 pe-4">${item}</span>`
    )
    .join("");

  return `${radioOptions} ${
    element.name == "Relation"
      ? `<input type="text" class="form-control mb-2" id="${element.label}" name="${element.label}"  placeholder="${element.label}" />`
      : ""
  }`;
}
function CreateSelectField(element) {
  const options = element.options ?? [];
  const list = options.map(
    (item) => `<option value="${item}">${item}</option>`
  );
  return `<select class="form-select mb-2" name="${element.name}" id="${element.name}">${list}</select>`;
}
function GenerateForm(Form) {
  const formContainer = $("#form");

  Form.forEach((item, itemIndex) => {
    const { section, fields } = item;
    const row = $(`
      <div class="row mt-3 mb-3">
        <p class="text-center fs-3 fw-bold">${section}</p>
      </div>
    `);

    fields.forEach((element, fieldIndex) => {
      const validationFunctions =
        element.validationFunctions?.map((fn) => validationFunctionsList[fn]) ||
        [];
      if (validationFunctions.length) {
        attachValidation(element, validationFunctions);
      }

      let input;
      switch (element.type) {
        case "select":
          input = CreateSelectField(element);
          break;
        case "radio":
          input = CreateRadioField(element);
          break;
        default:
          input = CreateInputField(element);
          break;
      }

      const isCheckbox = element.type === "checkbox";
      const label =
        element.type !== "radio" ? `<label>${element.label}</label>` : "";
      const controls =
        itemIndex !== 4
          ? `
        <div class="d-flex justify-content-end">
          <div class="dropdown">
            <i class="fa-solid fa-plus" role="button" data-bs-toggle="dropdown"></i>
            <ul class="dropdown-menu">
              <li>
                <a class="dropdown-item add-element" href="#" data-item-index="${itemIndex}" data-field-index="${fieldIndex}">
                  Add a new element
                </a>
              </li>
              ${
                element.isFormSpecific
                  ? `
                <li>
                  <a class="dropdown-item edit-element" href="#" data-item-index="${itemIndex}" data-field-index="${fieldIndex}">
                    Edit element
                  </a>
                </li>`
                  : ""
              }
              ${
                element.type === "select"
                  ? `
                <li>
                  <a class="dropdown-item add-options" href="#" data-item-index="${itemIndex}" data-field-index="${fieldIndex}">
                    Add More Options
                  </a>
                </li>`
                  : ""
              }
            </ul>
          </div> 
        </div>`
          : "";

      row.append(`
        <div class="${isCheckbox ? "col-sm-12" : "col-sm-6"} mt-2 mb-2">
          ${label}
          ${input || ""}
          ${controls}
        </div>
      `);
    });

    formContainer.append(row);

    if (section === "Documents") {
      formContainer.append(
        `<button class="btn btn-dark" id="addDocument">Add Document</button>`
      );
    }
  });

  GetDistricts();
}


$(document).ready(function () {
  const validtionOptions = [
    "notEmpty",
    "onlyAlphabets",
    "onlyDigits",
    "specificLength",
    "isAgeGreaterThan",
    "isEmailValid",
    "isDateWithinRange",
    "CapitalizeAlphabets",
  ];
  $("#tehsilLevel").on("click", function () {
    formStartingLevel = "Tehsil";
    // Remove the first field from Form[0].fields and store it
    let removedField = Form[0].fields.shift();

    // Add a new field at the beginning of Form[0].fields
    Form[0].fields.unshift({
      type: "select",
      label: "Select Tehsil Social Welfare Officer",
      name: "Tehsil",
      isFormSpecific: true,
    });

    // Add another new field at the beginning of Form[0].fields
    Form[0].fields.unshift({
      type: "select",
      label: "Select District",
      name: "District",
      isFormSpecific: true,
    });

    GenerateForm(Form);
    $("#FormChoice").remove();
    $("#Form").show();
  });
  $("#districtLevel").on("click", function () {
    formStartingLevel = "District";
    GenerateForm(Form);
    $("#FormChoice").remove();
    $("#Form").show();
  });
  $(document).on("change", 'select[name*="District"]', function () {
    const value = $(this).val();
    GetTehsil(value);
    GetBlock(value);
  });
  $(document).on("click", ".add-element", function (e) {
    e.preventDefault();
    const itemIndex = $(this).data("item-index");
    const fieldIndex = $(this).data("field-index");
    $("#itemIndex").val(itemIndex);
    $("#fieldIndex").val(fieldIndex);
    $("#addElementModal").modal("show");
  });
  $(document).on("click", ".edit-element", function (e) {
    e.preventDefault();
    const itemIndex = $(this).data("item-index");
    const fieldIndex = $(this).data("field-index");
    var item = Form[itemIndex].fields[fieldIndex];
    $("#itemIndex").val(itemIndex);
    $("#fieldIndex").val(fieldIndex);
    $("#elementType").val(item.type).trigger("change");
    $("#elementName").val(item.name);
    $("#elementLabel").val(item.label);
    if (item.options & (item.options.length > 0)) {
      item.options.forEach((item) => {
        $("#optionsList").append(
          `<div class="input-group mb-2"><input type="text" class="form-control optionInput" placeholder="Option value" value="${item}"><button class="btn btn-danger removeOptionButton" type="button">Remove</button></div>`
        );
      });
    }
    if (item.validationFunctions && item.validationFunctions.length > 0) {
      item.validationFunctions.forEach((item) => {
        $("#" + item)
          .prop("checked", true)
          .trigger("change");
      });
    }
    if (item.minLength) $("#minLength").val(item.minLength);
    if (item.maxLength) $("#maxLength").val(item.maxLength);
    $("#addElementModal form").append(
      `<input type="hidden" id="editElement"/>`
    );
    $("#addElementModal").modal("show");
  });
  // Show/Hide options input based on selected type
  $("#elementType").on("change", function () {
    if ($(this).val() === "select" || $(this).val() === "radio") {
      $("#optionsContainer").show();
    } else {
      $("#optionsContainer").hide();
    }

    $("#validationList").empty();

    if ($(this).val() == "date") {
      $("#validationList").append(`
        <div class="form-check">
            <input class="form-check-input" type="checkbox" value="isDateWithinRange"
                id="isDateWithinRange">
            <label class="form-check-label" for="isDateWithinRange">Date Within Range</label>
        </div>
      `);
    } else {
      validtionOptions.forEach((item) => {
        if (item != "isDateWithinRange")
          $("#validationList").append(`
          <div class="form-check">
              <input class="form-check-input" type="checkbox" value="${item}"
                  id="${item}">
              <label class="form-check-label" for="${item}">${convertToTitleCase(
            item
          )}</label>
          </div>
      `);
      });
    }
  });

  $(document).on("click", "#addDocument", function (e) {
    e.preventDefault();
    $("#addDocumentModal").modal("show");
  });
  // Add new option input
  $("#addOptionButton").on("click", function () {
    $("#optionsList").append(
      '<div class="input-group mb-2 center-align gap-1"><input type="text" class="form-control optionInput" placeholder="Option value"><i class="fa-solid fa-trash-can fs-6 text-danger removeOptionButton"></i><i class="fa-solid fa-pencil fs-6 text-primary"></i></div>'
    );
  });

  $("#addEnclosureOptionButton").on("click", function () {
    $("#enclosureOptionsList").append(
      '<div class="input-group mb-2 center-align gap-1"><input type="text" class="form-control optionEnclosureInput" placeholder="Option value"><i class="fa-solid fa-trash-can fs-6 text-danger removeEnclosureOptionButton"></i><i class="fa-solid fa-pencil fs-6 text-primary"></i></div>'
    );
  });
  // Remove option input
  $(document).on("click", ".removeOptionButton", function () {
    $(this).closest(".input-group").remove();
  });

  // Remove option input
  $(document).on("click", ".removeEnclosureOptionButton", function () {
    $(this).closest(".input-group").remove();
  });

  $("#saveElementButton").on("click", function () {
    const itemIndex = $("#itemIndex").val();
    const fieldIndex = $("#fieldIndex").val();
    const newElement = {
      type: $("#elementType").val(),
      name: $("#elementName").val(),
      label: $("#elementLabel").val(),
      isFormSpecific: true, // Set this according to your requirements
      options: [], // Initialize options array
      validationFunctions: [], // Initialize validation functions array
    };

    if (newElement.type === "select" || newElement.type === "radio") {
      $(".optionInput").each(function () {
        newElement.options.push($(this).val());
      });
    }

    $("#validationList .form-check-input:checked").each(function () {
      newElement.validationFunctions.push($(this).val());
    });

    if ($("#minLength").length && $("#minLength").val() != "")
      newElement["minLength"] = $("#minLength").val();
    if ($("#maxLength").length && $("#maxLength").val() != "")
      newElement["maxLength"] = $("#maxLength").val();

    if ($("#editElement").length == 1)
      Form[itemIndex].fields[fieldIndex] = newElement;
    else Form[itemIndex].fields.splice(parseInt(fieldIndex) + 1, 0, newElement);
    $("#addElementModal").modal("hide");
    $("#form").empty();
    GenerateForm(Form);
  });
  $("#saveDocumentButton").on("click", function () {
    const enlosure = {
      type: "select",
      name: $("#documentLabel").val().split(" ").join("") + "Enclosure",
      label: $("#documentLabel").val(),
      options: [], // Initialize options array
      validationFunctions: ["notEmpty"], // Initialize validation functions array
    };
    const file = {
      type: "file",
      name: $("#documentLabel").val().split(" ").join("") + "File",
      label: $("#documentLabel").val(),
      validationFunctions: ["notEmpty"],
      accept: ".pdf",
    };
    $(".optionEnclosureInput").each(function () {
      enlosure.options.push($(this).val());
    });

    Form[4].fields.push(enlosure);
    Form[4].fields.push(file);

    $("#addDocumentModal").modal("hide");
    $("#form").empty();
    GenerateForm(Form);
  });

  $(document).on("change", "#validationList .form-check-input", function () {
    const value = $(this).val();
    if (value == "specificLength")
      $("#isAgeGreaterThan").prop("checked", false);
    else if (value == "isAgeGreaterThan")
      $("#specificLength").prop("checked", false);

    if (value == "onlyAlphabets") $("#onlyDigits").prop("checked", false);
    else if (value == "onlyDigits") $("#onlyAlphabets").prop("checked", false);

    if (value == "specificLength" || value == "isAgeGreaterThan") {
      $("#maxLength").remove();
      $("#minLength").remove();
      $(this)
        .parent()
        .append(
          `<input class="form-control w-50" name="maxLenght" id="maxLength" placeholder="Max Length" required/>`
        );
    }

    if (value == "isDateWithinRange") {
      $("#maxLength").remove();
      $("#minLength").remove();
      $(this)
        .parent()
        .append(
          `<input class="form-control w-50" name="minLenght" id="minLength" placeholder="Min Length" required/><input class="form-control w-50" name="maxLenght" id="maxLength" placeholder="Max Length" required />`
        );
    }
  });
});
