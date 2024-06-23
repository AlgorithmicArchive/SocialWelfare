function DisplayHistory(applications) {
  $("#HistoryContainer").show();
  initializeDataTable('historyTable', 'historyList', applications);

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
