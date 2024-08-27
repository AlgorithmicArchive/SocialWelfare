function PullApplication(applicationId) {
  const formData = new FormData();
  formData.append("ApplicationId", applicationId);
  fetch("/Officer/PullApplication", { method: "post", body: formData })
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        window.location.href = "/Officer/Index";
      }
    });
}

function UserDetails(applicationId) {
  console.log(applicationId);
  window.location.href = "/Officer/UserDetails?ApplicationId=" + applicationId;
}

async function SanctionAll(poolList, finalList, ServiceId) {
  if (finalList.length > 0) {
    const id = finalList.shift();
    showSpinner();
    const formdata = new FormData();
    formdata.append("ApplicationId", id);
    formdata.append("Action", "Sanction");
    formdata.append("Remarks", "Sanctioned");
    formdata.append("serviceId", ServiceId);
    const response = await fetch("/Officer/HandleAction", {
      method: "post",
      body: formdata,
    });
    const data = await response.json();
    if (data.status) {
      hideSpinner();
    }

    $("#showSanctionLetter").modal("show");

    $("#sanctionFrame").attr(
      "src",
      "/files/" + id.replace(/\//g, "_") + "SanctionLetter.pdf"
    );
    $("#approve")
      .off("click")
      .on("click", async function () {
        fetch("/Officer/SignPdf?ApplicationId=" + id)
          .then((res) => res.json())
          .then((data) => console.log(data));

        $("#showSanctionLetter").modal("hide");
        SanctionAll(poolList, finalList, ServiceId); // Recall the function with the modified array
      });
  } else {
    const formdata = new FormData();
    formdata.append("IdList", JSON.stringify(finalList));
    formdata.append("serviceId", ServiceId);
    formdata.append("listType", "Pool");
    formdata.append("action", "remove");
    fetch("/Officer/UpdatePool", { method: "post", body: formdata })
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          hideSpinner();
          window.location.href = "/Officer/Index";
        }
      });
  }
}
