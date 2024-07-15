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
  return `
        <div class="col-sm-6 d-flex flex-column">
            <label for="${id}">${label}</label>
            <input class="form-control mb-2" type="text"  value="${value}" readonly />
        </div>`;
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
    $("showPreviousActions").show();
    previousActions.map((item) => {
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
function ProceedAction(applicationId, officer) {
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
  }
  if (action == "Update") {
    const updateColumn = {
      name: $("#extra input").attr("name"),
      value: $("#extra input").val(),
    };
    const serviceSpecific = JSON.parse(
      ApplicationDetails.generalDetails.serviceSpecific
    );
    if (serviceSpecific.hasOwnProperty(updateColumn.name)) {
      serviceSpecific[updateColumn.name] = updateColumn.value;
    }

    formdata.append("UpdateColumn", "ServiceSpecific");
    formdata.append("UpdateColumnValue", JSON.stringify(serviceSpecific));
  }
  fetch("/Officer/Action", { method: "post", body: formdata })
    .then((res) => res.json())
    .then((data) => {
      hideSpinner();
      if (data.status) {
        if (action == "Sanction") {
          $("#closeExmpleModal").trigger("click");
          $(".full-screen-section").empty();
          const filePath =
            "/files/" +
            data.applicationId.replace(/\//g, "_") +
            "SanctionLetter.pdf";
          const embed = `<embed src="${filePath}" type="application/pdf" width="800" height="600">`;
          $(".full-screen-section").append(`
            <p class="fw-bold text-center">This Application is sanctioned by you.</p>
            ${embed}
          `);
        } else window.location.href = data.url;
      }
    });
}
function getWorkForceOfficer(serviceContent, currentOfficer, applicationId) {
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
          ProceedAction(applicationId, currentOfficer);
        })
    );
}
