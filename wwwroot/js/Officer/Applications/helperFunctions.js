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

async function SanctionAll(poolList, ServiceId) {
  for (let i = 0; i < poolList.length; i++) {
    console.log(i);
    hideSpinner();
    const userApproved = confirm(
      `Do you want to issue sanction letter for Reference Number ${poolList[i]}?`
    );
    showSpinner();
    if (userApproved) {
      const formdata = new FormData();
      formdata.append("ApplicationId", poolList[i]);
      formdata.append("Action", "Sanction");
      formdata.append("Remarks", "Sanctioned");
      const response = await fetch("/Officer/Action", {
        method: "post",
        body: formdata,
      });
      const data = await response.json();
      if (data.status) {
        hideSpinner();
        console.log(data.url);
      }
    }
  }

  const formdata = new FormData();
  formdata.append("poolIdList", JSON.stringify([]));
  formdata.append("serviceId", ServiceId);
  fetch("/Officer/UpdatePool", { method: "post", body: formdata })
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        hideSpinner();
        window.location.href = "/Officer/Index";
      }
    });
}
