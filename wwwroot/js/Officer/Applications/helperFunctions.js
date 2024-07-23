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
    const response = await fetch("/Officer/Action", {
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
        $("#showSanctionLetter").modal("hide");
        SanctionAll(poolList, finalList, ServiceId); // Recall the function with the modified array
      });
  } else {
    const formdata = new FormData();
    formdata.append("poolIdList", JSON.stringify(poolList));
    formdata.append("serviceId", ServiceId);
    console.log("EMPTY", poolList);
    fetch("/Officer/UpdatePool", { method: "post", body: formdata })
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          hideSpinner();
          window.location.href = "/Officer/Applications?type=Pending";
        }
      });
  }
}
