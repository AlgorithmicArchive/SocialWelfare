$(document).ready(function () {
  let serviceId = 0;
  const services = serviceList;
  services.map((item) => {
    $("#services").append(
      `<option value="${item.serviceId}">${item.serviceName}</option>`
    );
  });

  $("#getRecords").on("click", function () {
    const servicesInput = $("#services");
    serviceId = servicesInput.val();

    if (!serviceId) {
      if (!servicesInput.next("span.text-danger").length) {
        servicesInput.after(
          `<span class="text-danger" style="font-size:12px;">This field is required</span>`
        );
      }
    } else {
      servicesInput.siblings("span").remove();
      showSpinner();
      fetch("/Officer/IsBankFile?serviceId=" + serviceId)
        .then((res) => res.json())
        .then((data) => {
          hideSpinner();
          if (data.isFilePresent) {
            $("#ftpForm").show();
          } else {
            $("#ftpContainer").append(
              `<p class="w-100 mt-5 fs-3 text-center">No file present please create a file first.</p>`
            );
          }
        });
    }

    $("#ftpForm").on("submit", function (e) {
      e.preventDefault();

      if (this.checkValidity()) {
        const formData = new FormData(this);
        formData.append("serviceId", serviceId);
        showSpinner();
        fetch("/Officer/UploadCsv", { method: "post", body: formData })
          .then((res) => res.json())
          .then((data) => {
            if (data.status) {
              $(this).append(
                `<p class="text-center fs-4 text-success">${data.message}</p>`
              );
              setTimeout(() => {
                window.location.href = "/Officer";
              }, 2000);
            }
          });
        hideSpinner();
      } else {
        console.log("Form is invalid");
      }
    });
  });
});
