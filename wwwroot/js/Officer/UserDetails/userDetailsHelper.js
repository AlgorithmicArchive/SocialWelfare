// Utility functions
function openInIframe(url) {
  $("#myIframe").attr("src", url);
  $("#showDoc").click();
}
function camelCaseToTitleCase(input) {
  if (typeof input !== "string" || input.length === 0) return input;
  const result = input.replace(/([A-Z])/g, (match, offset) =>
    offset === 0 ? match : " " + match
  );
  return result.charAt(0).toUpperCase() + result.slice(1);
}
function isDigit(input) {
  return /^\d+$/.test(input.toString().replace(/\s/g, ""));
}
function createInputElement(label, id, value) {
  if (updateColumn.name == id) updateColumn.value = value;
  let updated = false;
  if (updateObject.ColumnName == id) {
    value = updateObject.OldValue;
    updated = true;
  }
  return `
        <div class="col-sm-6 d-flex flex-column">
            <label for="${id}">${label}${
    updated ? " given by Citizen " : ""
  }</label>
            <input class="form-control mb-2" type="text"  value="${value}" readonly />
        </div>
        ${
          updated
            ? `
           <div class="col-sm-6 d-flex flex-column">
            <label for="${id}" class="text-danger fw-bold">${label} Updated By ${updateObject.Officer}</label>
            <input class="form-control mb-2" type="text"  value="${updateObject.NewValue}" readonly />
           </div>
          
          `
            : ``
        }
        `;
}
function createDocumentInput(label, enclosure, file) {
  return `
        <div class="col-sm-6 d-flex flex-column">
            <label for="${label}">${camelCaseToTitleCase(label)}</label>
            <a href="#" onclick="openInIframe('${file}'); return false;">${enclosure}</a>
        </div>`;
}
function appendDetails(containerId, details, prefix = "") {
  for (const item in details) {
    if (
      details[item] != null &&
      (item === "pincode" || !isDigit(details[item]))
    ) {
      const label = camelCaseToTitleCase(item);
      const inputElement = createInputElement(
        prefix + label,
        item,
        details[item]
      );
      $(`#${containerId}`).append(inputElement);
    }
  }
}
function appendGeneralDetails(generalDetails, excludedProperties) {
  for (const item in generalDetails) {
    if (
      generalDetails[item] != null &&
      !isDigit(generalDetails[item]) &&
      !excludedProperties.includes(item)
    ) {
      const label = camelCaseToTitleCase(item);
      let inputElement = "";

      if (item === "serviceSpecific") {
        const serviceSpecific = JSON.parse(generalDetails[item]);
        for (const each in serviceSpecific) {
          if (!isDigit(serviceSpecific[each])) {
            const subLabel = camelCaseToTitleCase(each);
            inputElement += createInputElement(
              subLabel,
              each,
              serviceSpecific[each]
            );
          }
        }
      } else {
        inputElement = createInputElement(label, item, generalDetails[item]);
      }

      $("#personalDetails").append(inputElement);
    }
  }
}
function appendBankDetails(bankDetails) {
  for (const item in bankDetails) {
    const label = camelCaseToTitleCase(item);
    const inputElement = createInputElement(label, item, bankDetails[item]);
    $("#bankDetails").append(inputElement);
  }
}
function appendDocuments(documents) {
  for (const item in documents) {
    const inputElement = createDocumentInput(
      documents[item].Label,
      documents[item].Enclosure,
      documents[item].File
    );
    $("#documents").append(inputElement);
  }
}
function appendPerviousActions(previousActions) {
  if (previousActions.length > 0) {
    $("#showPreviousActions").show();
    previousActions.reverse().map((item) => {
      $("#previousActions").append(`
        <tr>
          <td>${item.officer}</td>
          <td>${item.actionTaken}</td>
          <td>${item.remarks}</td>
        </tr>
    `);
    });
  }
}
function getFormattedDateTime() {
  const now = new Date();
  const options = {
    month: "numeric",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "numeric",
    second: "numeric",
    hour12: true,
  };

  return now.toLocaleString("en-US", options);
}

function formatDate() {
  const date = new Date();
  const options = { day: "2-digit", month: "short", year: "numeric" };
  const dateString = date.toLocaleDateString("en-US", options);

  const timeOptions = { hour: "2-digit", minute: "2-digit", hour12: true };
  const timeString = date.toLocaleTimeString("en-US", timeOptions);

  return `${dateString} ${timeString}`;
}
function ProceedAction(applicationId, officer, letterUpdateDetails) {
  const action = $("#action").val();
  const remarks = $("#remarks").val();
  if (remarks.length == 0) {
    $("#remarks").after(`<span class="text-danger">This is required.</span>`);
    return;
  }
  showSpinner();
  const formdata = new FormData();
  formdata.append("ApplicationId", applicationId);
  formdata.append("Officer", officer);
  formdata.append("Action", action);
  formdata.append("Remarks", remarks);

  const editList = [];

  if (action == "ReturnToEdit") {
    $(".editColumn:checked").each(function () {
      editList.push($(this).val());
    });

    formdata.append("editList", JSON.stringify(editList));
  } else if (action == "Update") {
    const updateColumn = {
      name: $("#extra input").attr("name"),
      value: $("#extra input").val(),
    };
    formdata.append("UpdateColumn", updateColumn.name);
    formdata.append("UpdateColumnValue", updateColumn.value);
  } else if (action == "Sanction") {
    const obj = {};
    letterUpdateDetails.forEach((item) => {
      const value = $("#new" + item.name).val();
      if (value != "") {
        obj[item.name] = {
          OldValue: $("#" + item.name).val(),
          NewValue: $("#new" + item.name).val(),
          UpdatedBy: ApplicationDetails.currentOfficer,
          UpdatedAt: formatDate(),
        };
      }
    });
    formdata.append("letterUpdateDetails", JSON.stringify(obj));
  }
  fetch("/Officer/Action", { method: "post", body: formdata })
    .then((res) => res.json())
    .then((data) => {
      hideSpinner();
      if (data.status) {
        if (action == "Sanction") {
          const filePath =
            "/files/" +
            data.applicationId.replace(/\//g, "_") +
            "SanctionLetter.pdf";

          $("#showSanctionLetter").modal("show");

          $("#sanctionFrame").attr("src", filePath);
          $("#approve")
            .off("click")
            .on("click", async function () {
              $("#showSanctionLetter").modal("hide");
              window.location.href = "/Officer/Index";
            });
        } else window.location.href = data.url;
      }
    });
}
function getWorkForceOfficer(
  serviceContent,
  currentOfficer,
  applicationId,
  letterUpdateDetails
) {
  const workForceOfficers = JSON.parse(serviceContent.workForceOfficers);
  let officer;
  workForceOfficers.map((item) => {
    if (item.Designation == currentOfficer) {
      officer = item;
    }
  });

  const options = [
    officer.canForward
      ? `<option value="Forward">Forward To ${officer.nextOfficer}</option>`
      : "",
    officer.canReturn
      ? `<option value="Return">Return To ${officer.prevOfficer}</option>`
      : "",
    officer.canReturnToEdit
      ? `<option value="ReturnToEdit">Return To Edit</option>`
      : "",
    officer.canSanction
      ? `<option value="Sanction">Issue Sanction Letter</option>`
      : "",
    officer.canUpdate
      ? `<option value="Update">Update and Forward To ${officer.nextOfficer}</option>`
      : "",
    `<option value="Reject">Reject</option>`,
  ];
  const validOptions = options.filter((option) => option !== "").join("");
  $("#action").append(validOptions);
  $("#action")
    .parent()
    .append(
      $("<label>").attr("for", "Remarks").text("Remarks"),
      $("<input>").addClass("form-control").attr({
        type: "text",
        placeholder: "Remarks",
        name: "remarks",
        id: "remarks",
        required: true,
      }),
      $("<button>")
        .addClass("btn btn-dark")
        .text("Proceed")
        .on("click", function () {
          ProceedAction(applicationId, currentOfficer, letterUpdateDetails);
        })
    );
}

function convertToCamelCase(input) {
  if (!input || typeof input !== "string") {
    return "";
  }

  return input.charAt(0).toLowerCase() + input.slice(1);
}

function attachValidations(obj) {
  let Obj = JSON.parse(JSON.stringify(obj));
  Obj.name = "new" + obj.name;
  const validationFunctions =
    obj.validationFunctions?.map((item) => validationFunctionsList[item]) || [];
  if (validationFunctions.length) {
    attachValidation(Obj, validationFunctions);
  }
}

function CertificateDetails(letterUpdateDetails, generalDetails) {
  $("#ceritificateDetails").show();
  $("#ceritificateDetails").empty();

  letterUpdateDetails.forEach((item) => {
    const type = item.type == "date" ? "text" : item.type;
    let value;
    if (generalDetails.hasOwnProperty(convertToCamelCase(item.name))) {
      value = generalDetails[convertToCamelCase(item.name)];
    } else {
      const serviceSpecific = JSON.parse(generalDetails.serviceSpecific);
      if (serviceSpecific.hasOwnProperty(item.name)) {
        value = serviceSpecific[item.name];
      }
    }
    $("#ceritificateDetails").append(`
          <div class="row mt-2">
              <div class="col-sm-6">
                <label>${item.label}</label>
                <input type="${type}" class="form-control" name="${item.name}" id="${item.name}" value="${value}" disabled/>
              </div>
              <div class="col-sm-6">
                <label>New ${item.label}</label>
                <input type="${type}" class="form-control" name="new${item.name}" id="new${item.name}" />
              </div>
          </div>
      `);

    attachValidations(item);
  });
}
