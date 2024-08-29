function downloadFile(filePath) {
  fetch(filePath)
    .then((response) => response.blob())
    .then((blob) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.style.display = "none";
      a.href = url;
      a.download = filePath.split("/").pop(); // Extract filename from path
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
    })
    .catch((error) => console.error("Error downloading file:", error));
}

function startFileCreation(serviceId, districtId) {
  $("#progress-container").show();
  fetch(`/Officer/BankCsvFile?serviceId=${serviceId}&districtId=${districtId}`)
    .then((response) => {
      if (!response.ok) {
        throw new Error("Network response was not ok");
      }
      return response.json(); // Assuming the server returns JSON
    })
    .then((data) => {
      $("#msg").remove();
      $("#generateBankFile").remove();
      $("#mainContainer").append(
        `<p class="text-center text-success fs-5">File Created Successfully.</p>
        <button class="btn text-primary d-flex mx-auto" onclick='downloadFile("${data.filePath}")'>Download file</button>
        <button class="btn btn-dark d-flex mx-auto"  id="sendToSFTP">Send To SFTP</button>`
      );
    })
    .catch((error) => {
      alert("Error creating file.");
      console.error("There was a problem with the fetch operation:", error);
    });
}

$(document).ready(function () {
  let serviceId = 0;
  let districtId = 0;
  const services = data.serviceList;
  const districts = data.districts;
  let bankFile;

  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/progressHub")
    .build();

  connection.on("ReceiveProgress", function (progress) {
    console.log(`Progress: ${progress}%`);
    // Update progress bar or any UI element
    document.getElementById("progressBar").value = progress;
  });

  connection.start().catch(function (err) {
    return console.error(err.toString());
  });

  services.map((item) => {
    $("#services").append(
      `<option value="${item.serviceId}">${item.serviceName}</option>`
    );
  });

  districts.map((item) => {
    $("#districts").append(
      `<option value="${item.districtId}">${item.districtName}</option>`
    );
  });

  $("#getRecords").on("click", function () {
    const servicesInput = $("#services");
    const districtInput = $("#districts");
    serviceId = servicesInput.val();
    districtId = districtInput.val();

    if (serviceId == 0) {
      if (!servicesInput.next("span.text-danger").length) {
        servicesInput.after(
          `<span class="text-danger" style="font-size:12px;">This field is required</span>`
        );
      }
    } else if (districtId == 0) {
      if (!districtInput.next("span.text-danger").length) {
        districtInput.after(
          `<span class="text-danger" style="font-size:12px;">This field is required</span>`
        );
      }
    } else {
      servicesInput.siblings("span.text-danger").remove();
      districtInput.siblings("span.text-danger").remove();
      showSpinner();

      fetch(
        `/Officer/IsBankFile?serviceId=${serviceId}&districtId=${districtId}`
      )
        .then((res) => res.json())
        .then((data) => {
          console.log(data);
          hideSpinner();
          bankFile = data.bankFile;
          if (data.bankFile != null) {
            if (data.newRecords == 0) {
              $("#mainContainer").append(
                `<p id="msg" class="w-100 mt-5 fs-3 text-center">A file exists with ${bankFile.totalRecords} records</p><button class="btn btn-dark d-flex mx-auto mt-2" id="sendToSFTP">Send To SFTP</button>`
              );
            } else
              $("#mainContainer").append(
                `<p id="msg" class="w-100 mt-5 fs-3 text-center">A file already exists with ${bankFile.totalRecords} records</p>
               <button class="btn btn-dark d-flex mx-auto" id="generateBankFile">Append To File</button>
              `
              );
          } else if (data.newRecords == 0) {
            $("#mainContainer").append(
              `<p id="msg" class="w-100 mt-5 fs-3 text-center">No Sanctioned Applications in this district</p>`
            );
          } else {
            $("#mainContainer").append(
              `<p id="msg" class="w-100 mt-5 fs-3 text-center">No file present</p>
               <button class="btn btn-dark d-flex mx-auto" id="generateBankFile">Generate File</button>
              `
            );
          }
        });
    }
  });

  $(document).on("click", "#generateBankFile", function () {
    startFileCreation(serviceId, districtId);
  });

  $(document).on("click", "#sendToSFTP", function () {
    $("#ftpCredentials").modal("show");
  });

  $("#send")
    .off("click")
    .on("click", async function () {
      $("#ftpCredentials input").each(function () {
        if ($(this).val() == "" && $(this).next("span").length == 0) {
          $(this).after('<span class="errorMsg">This field is required</span>');
        } else if ($(this).val() != "") {
          $(this).next("span").remove();
        }
      });

      const canProceed = $(".errorMsg").length == 0;

      if (canProceed) {
        const formdata = new FormData();
        formdata.append("ftpHost", $("#ftpHost").val());
        formdata.append("ftpUser", $("#ftpUser").val());
        formdata.append("ftpPassword", $("#ftpPassword").val());
        formdata.append("serviceId", serviceId);
        formdata.append("districtId", districtId);
        showSpinner();
        fetch("/Officer/UploadCsv", { method: "POST", body: formdata })
          .then((res) => res.json())
          .then((data) => {
            hideSpinner();
            if (data.status) {
              $("#send").after(
                `<p class="text-success text-center">${data.message}</p>`
              );
              setTimeout(() => {
                window.location.href = "/Officer";
              }, 2000);
            } else {
              $("#send").after(
                `<p class="errorMsg text-center">${data.message}</p>`
              );
            }
          });
      }
    });
});
