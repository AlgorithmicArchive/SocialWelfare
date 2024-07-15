function EditForm(ApplicationId, returnedToEdit = false) {
  let url = "/User/ServiceForm?ApplicationId=" + ApplicationId;
  if (returnedToEdit) url += "&returnToEdit=" + returnedToEdit;
  window.location.href = url;
}

function downloadFile(ApplicationId) {
  const filePath =
    "/files/" + ApplicationId.replace(/\//g, "_") + "SanctionLetter.pdf";
  fetch(filePath)
    .then((response) => response.blob())
    .then((blob) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.style.display = "none";
      a.href = url;
      a.download = filePath.split("/").pop(); // Extract filename from path
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
    })
    .catch((error) => console.error("Error downloading file:", error));
}

function CreateTimeline(phase, ApplicationId) {
  let sanctioned = false;
  let returnedToEdit = false;
  $("#timeline").empty();
  $("#ApplicationId").remove();
  $("#timeline")
    .parent()
    .parent()
    .prepend(
      `<p class="fw-bold text-center" id="ApplicationId">Application Number: ${ApplicationId}</p>`
    );
  for (let i = 0; i < phase.length; i++) {
    const item = phase[i];
    const [date, time] = item.ReceivedOn.split(" ");
    sanctioned = item.ActionTaken == "Sanction" ? true : false;
    returnedToEdit = item.ActionTaken == "ReturnToEdit" ? true : false;
    const tr = $("<tr/>");
    tr.append(`
        <td>${date} at ${time}</td>
        <td>${item.Officer}</td>
        <td>${item.ActionTaken == "" ? "Pending" : item.ActionTaken}</td>
        <td>${item.Remarks}</td>
    `);
    $("#timeline").append(tr);
    if (item.HasApplication || item.ActionTaken == "ReturnToEdit") {
      break;
    }
  }

  if (returnedToEdit) {
    $("#statusButtons").find("editFormButton").remove();
    $("#statusButtons").prepend(
      `<button class="btn btn-light" id="UpdateFormButton"
                onclick='EditForm("${ApplicationId}",${returnedToEdit})'>Edit Form</button>`
    );
  }
  if (sanctioned) {
    $("#statusButtons").find("downloadLetter").remove();
    $("#statusButtons").prepend(
      `<button class="btn btn-light" id="downloadLetter"
                onclick='downloadFile("${ApplicationId}")'>Downlaod Sanction Letter</button>`
    );
  }
}

function SendUpdateRequest(updateRequest, ApplicationId) {
  const newValue = $("#" + updateRequest.formElement.name).val();
  updateRequest.newValue = newValue;
  updateRequest.requested = 1;
  const formdata = new FormData();
  formdata.append("ApplicationId", ApplicationId);
  formdata.append("updateRequest", JSON.stringify(updateRequest));
  fetch("/User/UpdateRequest", { method: "post", body: formdata })
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        window.location.href = "/User/ApplicationStatus";
      }
    });
}

function UpdateRequest(updateRequest, ApplicationId) {
  const container = $("#updateRequestContainer");
  const obj = updateRequest.formElement;
  container.append(`
      <label for="${obj.label}">${obj.label}</label>
      <input type="${obj.type}" class="form-control" name="${obj.name}" id="${
    obj.name
  }"
               placeholder="${obj.label}" maxlength="${
    obj.maxLength
  }" accept="${obj.accept}" value="${obj.value}" />
      <button class="btn btn-dark mt-2 mx-auto" onclick='SendUpdateRequest(${JSON.stringify(
        updateRequest
      )},"${ApplicationId}")' >Request</button>
  `);
  const validationFunctions =
    obj.validationFunctions?.map((item) => validationFunctionsList[item]) || [];
  if (validationFunctions.length) {
    attachValidation(obj, validationFunctions);
  }
}

$(document).ready(function () {
  const Incomplete =
    window.location.pathname == "/User/IncompleteApplications" ? true : false;

  let list = [];
  list = applications.map(({ applicationId, applicantName, phase }) => {
    const Phase = phase !== "" ? JSON.parse(phase) : "";
    return {
      applicationId,
      applicantName,
      button: Incomplete
        ? `<button class="btn btn-dark w-100" onclick='EditForm("${applicationId}")'>Edit Form</button>`
        : `<button class="btn btn-dark w-100" data-bs-toggle="modal" data-bs-target="#exampleModal" onclick='CreateTimeline(${JSON.stringify(
            Phase
          )},"${applicationId}")'>View</button>`,
    };
  });

  initializeDataTable("statusTable", "userDetails", list);
});
