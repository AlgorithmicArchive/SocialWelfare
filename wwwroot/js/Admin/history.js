function printDiv(divId) {
  var content = $("#" + divId).html();

  // Desired window size
  var width = 1080;
  var height = 600;

  // Calculate the position for centering the window
  var left = (screen.width - width) / 2;
  var top = (screen.height - height) / 2;

  var myWindow = window.open(
    "",
    "",
    `width=${width},height=${height},top=${top},left=${left}`
  );
  myWindow.document.write("<html><head><title>Print</title>");
  myWindow.document.write(`<style>
        table { border-collapse: collapse; }
        th, td { border: 2px solid black; padding: 8px; text-align: left; }
        thead { background-color: #f2f2f2; }
        th { border: 1px solid black; }
        @media print {
            @page {
                size: landscape;
            }
        }
    </style>`);
  myWindow.document.write("</head><body>");
  myWindow.document.write(content);
  myWindow.document.write("</body></html>");
  myWindow.document.close();
  myWindow.focus();
  myWindow.print();
  myWindow.close();
}

function DisplayHistory(applications) {
  console.log(applications);
  $("#HistoryContainer").show();

  // Destroy the existing DataTable if it exists
  if ($.fn.DataTable.isDataTable("#historyTable")) {
    $("#historyTable").DataTable().destroy();
  }

  const container = $("#historyList");
  container.empty();
  applications.map((item, index) => {
    container.append(`
      <tr>
        <td>${index + 1}</td>
        <td>${item.ApplicationNo}</td>
        <td>${item.ApplicantName}</td>
        <td>${item.DateTime}</td>
        <td>${item.PreviouslyWith}</td>
        <td>${item.PreviousStatus}</td>
        <td>${item.AppliedDistrict}</td>
        <td>${item.AppliedService}</td>
        <td>${item.CurrentlyApplicationWithOfficer}</td>
        <td>${item.CurrentApplicationStatus}</td>
      </tr>
    `);
  });

  // Reinitialize the DataTable
  $("#historyTable").DataTable({
    paging: true,
    searching: true,
    info: true,
    lengthChange: true,
    pageLength: 10, // Default number of entries to display
    lengthMenu: [5, 10, 25, 50, 100], // Options for the user to select from
  });
}

$(document).ready(function () {
  $("#getRecords").click(function () {
    const fields = [
      { id: "#StartDate", errorMsg: "This field is required" },
      { id: "#EndDate", errorMsg: "This field is required" },
      { id: "#status", errorMsg: "This field is required" },
    ];

    let valid = true;

    fields.forEach(function (field) {
      const value = $(field.id).val();
      if (value == "") {
        if (!$(field.id).next(".errorMsg").length) {
          $(field.id).after(`<p class="errorMsg">${field.errorMsg}</p>`);
        }
        valid = false;
      } else {
        $(field.id).next(".errorMsg").remove();
      }
    });

    if (valid) {
      const startdate = $("#StartDate").val().split("/").join(" ");
      const enddate = $("#EndDate").val().split("/").join(" ");
      const status = $("#status").val();
      fetch(
        "/Admin/GetHistories?StartDate=" +
          startdate +
          "&EndDate=" +
          enddate +
          "&Status=" +
          status
      )
        .then((res) => res.json())
        .then((data) => {
          if (data.status) DisplayHistory(data.applications);
        });
      console.log("Form is valid. Proceed with getting records.");
    } else {
      console.log("Form is invalid. Correct the errors.");
    }
  });

  var currentYear = new Date().getFullYear();
  $("input[name*=Date]").datepicker({
    dateFormat: "dd/M/yy",
    changeMonth: true,
    changeYear: true,
    yearRange: "1990:" + currentYear,
  });
});
