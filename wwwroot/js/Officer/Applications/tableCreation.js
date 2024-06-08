function Table(container, tbody) {
  $("#" + container).append(`<tr>${tbody}</tr>`);
}
function createUpdateListRow(element) {
  const updateRequest = JSON.parse(element.updateRequest);
  return `
    <tr>
      <td>${element.applicationId}</td>
      <td>${element.applicantName}</td>
      <td>${updateRequest.formElement.label}</td>
      <td>${updateRequest.newValue}</td>
    </tr>
  `;
}
function PendingTable(Applications) {
  $("#tbody").empty();
  const canSanction = Applications.canSanction;
  if (
    canSanction &&
    Applications.PendingList.length > 0 &&
    $(".parent-pool").length == 0
  ) {
    $("#tbody")
      .parent()
      .find("thead tr")
      .prepend(
        `<td class="d-flex gap-2"><input type="checkbox" class="form-check parent-pool" />Pool</td>`
      );
  }
  const tbody =
    Applications.PendingList.length > 0
      ? Applications.PendingList.map(
          (element, index) =>
            ` <tr>
                ${
                  canSanction &&
                  `<td><input type="checkbox" class="form-check pool" value="${element.applicationId}" name="pool" /></td>`
                }
                <td>${index + 1}</td>
                <td>${element.applicationId}</td>
                <td>${element.applicantName}</td>
                <td><button class="btn btn-dark w-100" onclick='UserDetails("${
                  element.applicationId
                }");'>View</button></td>
            </tr>`
        ).join("")
      : `<tr><td colspan="4" class="fw-bold text-center">NO RECORD</td></tr>`;

  Table("tbody", tbody);

  if (Applications.UpdateList.length > 0) {
    $("#updateContainer").show();
    const updateListTbody = Applications.UpdateList.map((element) =>
      createUpdateListRow(element)
    ).join("");
    $("#updateListTbody")
      .parent()
      .parent()
      .prepend(
        `<p class="fs-4 fw-bold text-center">Applications yet to be updated</p>`
      );
    Table("updateListTbody", updateListTbody);
  }
}
function SentTable(Applications) {
  const tbody =
    Applications.SentApplications.length > 0
      ? Applications.SentApplications.map(
          (element, index) =>
            `<tr>
                <td>${index + 1}</td>
                <td>${element.applicationId}</td>
                <td>${element.applicantName}</td>
                <td>${
                  element.canPull
                    ? `<button class="btn btn-dark w-100" onclick='PullApplication("${element.applicationId}");'>Pull</button>`
                    : "Cannot Pull"
                }</td>
          </tr>
          `
        ).join("")
      : `<tr><td colspan="4" class="fw-bold text-center">NO RECORD</td></tr>`;

  Table("tbody", tbody);
}
function PoolTable(Applications) {
  $("#poolArray").empty();
  if (Applications.PoolList.length > 0) {
    const tbody = Applications.PoolList.map(
      (element, index) => `
      <tr>
        <td>${index + 1}</td>
        <td>${element.applicationId}</td>
        <td>${element.applicantName}</td>
      </tr>
  `
    );
    Table("poolArray", tbody);
  }
}

function MiscellaneousTable(Applications) {
  if (Applications.MiscellaneousList.length > 0) {
    const tbody = Applications.MiscellaneousList.map(
      (element, index) => `
      <tr>
        <td>${index + 1}</td>
        <td>${element.applicationId}</td>
        <td>${element.applicantName}</td>
        <td>NONE</td>
      </tr>
  `
    );
    Table("tbody", tbody);
  }
}
