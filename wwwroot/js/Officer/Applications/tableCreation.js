function Table(container, tbody) {
  $("#" + container).append(`<tr>${tbody}</tr>`);
}

function PendingTable(Applications) {
  $("#tbody").empty();
  const canSanction = Applications.canSanction;
  if (!canSanction) {
    $(".parent-pool").parent().parent().remove();
  }

  let pendingList = [];
  pendingList = Applications.PendingList.map((element) => {
    const checkboxColumn = canSanction
      ? `<input type="checkbox" class="form-check pool mx-auto" value="${element.applicationId}" name="pool" />`
      : "";

    let result = {};

    if (checkboxColumn) {
      result.checkbox = checkboxColumn;
    }

    result = {
      ...result, // Spread the existing properties (if any)
      applicationId: element.applicationId,
      applicantName: element.applicantName,
      button: `<button class="btn btn-dark w-100" onclick='UserDetails("${element.applicationId}");'>View</button>`,
    };
    return result;
  });
  initializeDataTable("applicationsTable", "tbody", pendingList);
}
function SentTable(Applications) {
  $(".parent-pool").parent().parent().remove();
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

  $("#tbody").empty();

  let pendingList = [];
  pendingList = Applications.SentApplications.map((element) => {
    result = {
      applicationId: element.applicationId,
      applicantName: element.applicantName,
      button: element.canPull
        ? `<button class="btn btn-dark w-100" onclick='PullApplication("${element.applicationId}");'>Pull</button>`
        : "Cannot Pull",
    };
    return result;
  });
  initializeDataTable("applicationsTable", "tbody", pendingList);
}
function PoolTable(Applications) {
  let list = [];
  list = Applications.PoolList.map(({ applicationId, applicantName }) => {
    return {
      checkbox: `<input type="checkbox" class="form-check mx-auto poolList-element" value="${applicationId}" />`,
      applicationId,
      applicantName,
    };
  });
  initializeDataTable("poolTable", "poolArray", list);
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
