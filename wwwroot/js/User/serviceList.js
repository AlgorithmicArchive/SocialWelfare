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
  const list = serviceList.map(({serviceName,department,serviceId})=>({serviceName,department,button:`<button class="btn btn-dark w-100" onclick='OpenForm(${serviceId})'>View</button>`}));
  initializeDataTable('serviceList', "tableBody", list);
});
