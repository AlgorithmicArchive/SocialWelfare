$(document).ready(function () {
  $("#createService").on("click", function () {
    if ($("#serviceName").val() == "") $("#serviceName").trigger("focus");
    else if ($("#departmentName").val() == "")
      $("#departmentName").trigger("focus");
    else {
      $("#FormInfo").hide();
      $("#FormChoice").show();
    }
  });
  $("#generateForm").on("click", function () {
    $("#Form").hide();
  });
});
