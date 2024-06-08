function InsertDetailRow(ApplicationId, ApplicantName, Index, updateRequest) {
  const ProertyToUpdate = updateRequest.formElement.label;
  const newValue = updateRequest.newValue;
  const container = $("#userDetails");
  const tr = $("<tr/>");
  tr.append(`
        <th scope="row">${Index}</th>
        <td>${ApplicationId}</td>
        <td>${ApplicantName}</td>
        <td>${ProertyToUpdate}</td>
        <td>${newValue}</td>
        <td><button class="btn btn-dark w-100" onclick='UpdateRequest("${ApplicationId}",${JSON.stringify(
    updateRequest
  )})'>Update</button></td>
       
  `);

  container.append(tr);
}

function UpdateRequest(ApplicationId, updateRequest) {
  console.log(ApplicationId, updateRequest);
  const formdata = new FormData();
  formdata.append("ApplicationId", ApplicationId);
  formdata.append("updateRequest", JSON.stringify(updateRequest));
  fetch("/Officer/UpdateRequests", { method: "post", body: formdata })
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        window.location.href = "/Officer/Index";
      }
    });
}

$(document).ready(function () {
  applications.map((item, index) => {
    const updateRequest = JSON.parse(item.updateRequest);
    const applicationId = item.applicationId;
    const applicantName = item.applicantName;
    InsertDetailRow(applicationId, applicantName, index + 1, updateRequest);
  });
});
