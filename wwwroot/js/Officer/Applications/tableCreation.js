function daysUntil(targetDateStr) {
  // Parse the target date string and create a Date object
  let targetDate = new Date(Date.parse(targetDateStr));
  // Create a new Date object for the current date
  let currentDate = new Date();

  // Set the time component of both dates to midnight (00:00:00)
  targetDate.setHours(0, 0, 0, 0);
  currentDate.setHours(0, 0, 0, 0);

  // Calculate the difference in milliseconds
  let differenceInMillis = targetDate - currentDate;
  // Convert the difference from milliseconds to days
  let millisecondsPerDay = 1000 * 60 * 60 * 24;
  let differenceInDays = Math.ceil(differenceInMillis / millisecondsPerDay);

  // Return the absolute difference in days
  return Math.abs(differenceInDays);
}

function PendingTable(Applications) {
  // Initialize the DataTable
  initializeRecordTables(
    "applicationsTable",
    "/Officer/Applications",
    "Pending",
    0,
    1
  );
}
function SentTable(Applications) {
  $("#tbody").empty();
  $(".pending-parent").parent().parent().remove();
  let sentList = [];
  sentList = Applications.SentApplications.map((element) => {
    console.log(element);
    result = {
      applicationId: element.applicationId,
      applicantName: element.applicantName,
      dateOfMarriage: element.dateOfMarriage,
      submissionDate:
        element.submissionDate +
        " (" +
        daysUntil(element.submissionDate) +
        " day(s))",
      button: element.canPull
        ? `<button class="btn btn-dark w-100" onclick='PullApplication("${element.applicationId}");'>Pull</button>`
        : "Cannot Pull",
    };
    return result;
  });
  initializeDataTable("applicationsTable", "tbody", sentList);
}
function PoolTable(Applications) {
  let list = [];
  list = Applications.PoolList.map(
    ({ applicationId, applicantName, dateOfMarriage, submissionDate }) => {
      return {
        checkbox: `<input type="checkbox" class="form-check poolList-element" value="${applicationId}" />`,
        applicationId,
        applicantName,
        dateOfMarriage,
        submissionDate:
          submissionDate + " (" + daysUntil(submissionDate) + " day(s))",
      };
    }
  );
  // initializeDataTable("poolTable", "poolArray", list, "Pool");
}

function ApproveTable(Applications) {
  let list = [];
  list = Applications.ApproveList.map(
    ({
      applicationId,
      applicantName,
      appliedDistrict,
      parentage,
      motherName,
      dateOfBirth,
      dateOfMarriage,
      bankDetails,
      address,
      submissionDate,
    }) => {
      return {
        checkbox: `<input type="checkbox" class="form-check approve-element" value="${applicationId}" />`,
        applicationId,
        applicantName,
        appliedDistrict,
        parentage,
        motherName,
        dateOfBirth,
        dateOfMarriage,
        bankDetails,
        address,
        submissionDate:
          submissionDate + " (" + daysUntil(submissionDate) + " day(s))",
      };
    }
  );

  // initializeDataTable("approveTable", "approveArray", list, "Approve");
}

function MiscellaneousTable(Applications) {
  $("#tbody").empty();
  $(".pending-parent").parent().parent().remove();
  let pendingList = [];
  pendingList = Applications.MiscellaneousList.map((element) => {
    result = {
      applicationId: element.applicationId,
      applicantName: element.applicantName,
      dateOfMarriage: element.dateOfMarriage,
      submissionDate:
        element.submissionDate +
        " (" +
        daysUntil(element.submissionDate) +
        " day(s))",
      button: element.canPull
        ? `<button class="btn btn-dark w-100" onclick='PullApplication("${element.applicationId}");'>Pull</button>`
        : "Cannot Pull",
    };
    return result;
  });
  initializeDataTable("applicationsTable", "tbody", pendingList);
}
