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
  // Update updateColumn if necessary
  if (updateColumn.name === id) {
    updateColumn.value = value;
    $("#extraDetails").empty().append(`
      <label>${updateColumn.label}</label>
      <input type="text" class="form-control" value="${updateColumn.value}" readonly/>
    `);
  }

  // Check if the updateObject should be used and prepare extraDetails content
  let updated = false;
  let extraDetailsContent = "";
  if (updateObject.ColumnName === id) {
    console.log(documents);
    value = updateObject.OldValue;
    updated = true;
    extraDetailsContent = `
      <label>${label} given By Citizen</label>
      <input type="text" class="form-control" value="${value}" readonly/>
      <label class="mt-1 text-danger fw-bold" title="${label} Updated By ${updateObject.Officer}">${label} updated by ${updateObject.Officer}</label>
      <input type="text" class="form-control" value="${updateObject.NewValue}" readonly/>
    `;
    $("#extraDetails").empty().append(extraDetailsContent);
    $("#documents").append(
      `<label class="text-danger fw-bold">${label} Updated By ${updateObject.Officer}</label><a href="#" onclick="openInIframe('${updateObject.File}'); return false;">View File</a>`
    );
  }

  // Construct the main input element
  let mainInputElement = `
    <div class="col-sm-6 d-flex flex-column">
      <label for="${id}">${label}${updated ? " given by Citizen" : ""}</label>
      <input class="form-control mb-2" type="text" value="${value}" readonly />
    </div>
  `;

  // Construct the updated input element if necessary
  let updatedInputElement = "";
  if (updated) {
    updatedInputElement = `
      <div class="col-sm-6 d-flex flex-column">
        <label for="${id}" class="text-danger fw-bold" title="${label} Updated By ${updateObject.Officer}">
          ${label} Updated By ${updateObject.Officer}
        </label>
        <a href="#" onclick="openInIframe('${updateObject.File}'); return false;">View File</a>
        <input class="form-control mb-2" type="text" value="${updateObject.NewValue}" readonly />
      </div>
    `;
  }

  // Return the combined HTML
  return mainInputElement + updatedInputElement;
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
          <td>${item.ActionTaker}</td>  
          <td>${item.ActionTaken}</td>
          <td>${item.DateTime}</td>
          <td>${item.Remarks}</td>
          <td>${
            item.File == ""
              ? "NIL"
              : `<a href="#" onclick="openInIframe('${item.File}'); return false;">View File</a>`
          }</td>
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
  const serviceId = ApplicationDetails.serviceContent.serviceId;
  const url = "/Officer/HandleAction";

  const formData = new FormData();
  formData.append("ApplicationId", applicationId);
  formData.append("Officer", officer);
  formData.append("Remarks", remarks);
  formData.append("ServiceId", serviceId);
  formData.append("Action", action);

  switch (action) {
    case "ReturnToEdit":
      const editList = $(".editColumn:checked")
        .map(function () {
          return $(this).val();
        })
        .get();
      formData.append("editList", JSON.stringify(editList));
      break;

    case "Update":
      const updateInput = $("#extra input");
      formData.append("UpdateColumn", updateInput.attr("name"));
      formData.append("UpdateColumnValue", updateInput.val());
      const updateFile = $("#" + updateInput.attr("name") + "File")[0].files[0];
      if (updateFile) formData.append("UpdateColumnFile", updateFile);
      break;

    case "Sanction":
      if(letterUpdateDetails){
        const updateDetails = {};
        letterUpdateDetails.forEach((item) => {
          updateDetails[item.name] = {
            OldValue: $("#" + item.name).val(),
            NewValue: $("#new" + item.name).val(),
            UpdatedBy: ApplicationDetails.currentOfficer,
            UpdatedAt: formatDate(),
          };
        });
        formData.append("letterUpdateDetails", JSON.stringify(updateDetails));
      }
     
      break;

    case "Forward":
      const forwardFile = $("#" + officer.replace(/\s+/g, "_") + "_Document");
      if (forwardFile.length != 0)
        formData.append("ForwardFile", forwardFile[0].files[0]);
      break;

    default:
      break;
  }

  showSpinner();
  fetch(url, { method: "post", body: formData })
    .then((res) => res.json())
    .then((data) => {
      hideSpinner();
      console.log(data);
      if (data.status) {
        if (action === "Sanction") {
          const id = data.applicationId;
          const filePath = `/files/${id.replace(/\//g, "_")}SanctionLetter.pdf`;

          console.log(filePath);
          $("#showSanctionLetter").modal("show");
          $("#sanctionFrame").attr("src", filePath);

          console.log("LENGTH", $("#approveSingle").length);

          $("#approveSingle").on("click", async function () {
            console.log("click", id);
            fetch("/Officer/SignPdf?ApplicationId=" + id)
              .then((res) => res.json())
              .then((data) => {
                console.log(data);
                $("#showSanctionLetter").modal("hide");
                $("#showSanctionLetter").modal("show");
                const parent  = $("#approveSingle").parent();
                $("#approveSingle").remove();
                parent.append(`<button class="btn btn-dark d-flex mx-auto" data-bs-dismiss="modal">OK</button>`)
                $("#sanctionFrame").attr("src", filePath);
              });

            // window.location.href = "/Officer/Index";
          });
        } else {
          window.location.href = "/Officer/Index";
        }
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
  let currentIndex;
  workForceOfficers.map((item,index) => {
    if (item.Designation == currentOfficer) {
      officer = item;
      currentIndex = index;
    }
  });

  const options = [
    officer.canForward
      ? `<option value="Forward">Forward To ${workForceOfficers[currentIndex+1].Designation}</option>`
      : "",
    officer.canReturn
      ? `<option value="Return">Return To ${workForceOfficers[currentIndex-1].Designation}</option>`
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
        .addClass("btn btn-dark d-flex mx-auto mt-2")
        .text("Proceed")
        .attr("type", "submit")
        .on("click", function (e) {
          e.preventDefault(); // Prevent form submission

          let form = $("#actionForm")[0]; // Get the form element
          if (!form.checkValidity()) {
            form.reportValidity(); // Trigger HTML5 validation
          } else {
            // All fields are valid, perform custom actions instead of form submission
            ProceedAction(applicationId, currentOfficer, letterUpdateDetails);
          }
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
  try {
    if (letterUpdateDetails && letterUpdateDetails.length > 0) {
      $("#ceritificateDetails").show();
      $("#ceritificateDetails").empty();
  
      $("#ceritificateDetails").append(
        `<p class="fs-3 fw-bold text-center">Certificate Details</p>`
      );
  
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
  } catch (error) {
    console.error("Error accessing letterUpdateDetails:", error);
  }
 
}
