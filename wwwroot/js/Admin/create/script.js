$(document).ready(function () {
  $("#createService").on("click", function () {
    if ($("#serviceName").val() == "") $("#serviceName").trigger("focus");
    else if ($("#departmentName").val() == "")
      $("#departmentName").trigger("focus");
    else {
      const formData = new FormData();
      formData.append("serviceName", $("#serviceName").val());
      formData.append("departmentName", $("#departmentName").val());
      fetch("/Admin/CreateService", { method: "post", body: formData })
        .then((res) => res.json())
        .then((data) => {
          if (data.status) serviceId = data.serviceId;
        });
      $("#FormInfo").hide();
      $("#FormChoice").show();
    }
  });
  $("#generateForm").on("click", function () {
    const formdata = new FormData();
    formdata.append("serviceId", serviceId);
    console.log(Form);
    formdata.append("formElements", JSON.stringify(Form));
    fetch("/Admin/UpdateService", { method: "post", body: formdata })
      .then((res) => res.json())
      .then((data) => data.status);
    $("#Form").hide();
    $("#WorkForceOfficer").show();
  });
});
