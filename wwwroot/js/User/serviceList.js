function OpenForm(serviceId) {
  const formdata = new FormData();
  formdata.append("serviceId", serviceId);

  fetch("/User/SetServiceForm", { method: "post", body: formdata })
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        window.location.href = data.url;
      }
    });
}

$(document).ready(function () {
  initializeRecordTables(
    "serviceList",
    "/User/GetServices",
    null,
    "Services",
    0,
    10
  );
});
