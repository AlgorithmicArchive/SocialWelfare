function daysUntil(targetDateStr) {
  let targetDate = new Date(Date.parse(targetDateStr));
  let currentDate = new Date();
  let differenceInMillis = targetDate - currentDate;
  let millisecondsPerDay = 1000 * 60 * 60 * 24;
  let differenceInDays = Math.ceil(differenceInMillis / millisecondsPerDay);
  return differenceInDays;
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
    let extra = {};
    if (checkboxColumn) {
      result.checkbox = checkboxColumn;
    }

    if (element.dateOfMarriage != null) {
      extra.dateOfMarriage = element.dateOfMarriage;
      $("#applicationsTable thead tr").empty();
      $("#applicationsTable thead tr").append(`
          <th scope="col">#</th>
          <th scope="col" class="">
              <span class="d-flex gap-2">
                  <input type="checkbox" class="form-check d-flex parent-pool" /><span>Pool</span>
              </span>
          </th>
          <th scope="col">Reference Number <i class="fa-solid fa-sort"></i></th>
          <th scope="col">Applicant Name <i class="fa-solid fa-sort"></i></th>
          <th scope="col">Date Of Marriage <i class="fa-solid fa-sort"></i></th>
          <th scope="col">Applicant Submission Date (Days Elapsed)<i class="fa-solid fa-sort"></i></th>
          <th scope="col">Action</th>
        `);

      if (!canSanction) {
        $(".parent-pool").parent().parent().remove();
      }
    }

    result = {
      ...result, // Spread the existing properties (if any)
      applicationId: element.applicationId,
      applicantName: element.applicantName,
      ...extra,
      submissionDate:
        element.submissionDate +
        " (" +
        daysUntil(element.submissionDate) +
        " days)",
      button: `<button class="btn btn-dark w-100" onclick='UserDetails("${element.applicationId}");'>View</button>`,
    };
    return result;
  });

  initializeDataTable("applicationsTable", "tbody", pendingList);
}
function SentTable(Applications) {
  $("#tbody").empty();
  $(".parent-pool").parent().parent().remove();
  console.log($(".parent-pool").parent().parent());
  let pendingList = [];
  pendingList = Applications.SentApplications.map((element) => {
    result = {
      applicationId: element.applicationId,
      applicantName: element.applicantName,
      submissionDate: element.submissionDate,
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
  $("#tbody").empty();
  $(".parent-pool").parent().parent().remove();
  let pendingList = [];
  pendingList = Applications.MiscellaneousList.map((element) => {
    result = {
      applicationId: element.applicationId,
      applicantName: element.applicantName,
      submissionDate: element.submissionDate,
      button: element.canPull
        ? `<button class="btn btn-dark w-100" onclick='PullApplication("${element.applicationId}");'>Pull</button>`
        : "Cannot Pull",
    };
    return result;
  });
  initializeDataTable("applicationsTable", "tbody", pendingList);
}
