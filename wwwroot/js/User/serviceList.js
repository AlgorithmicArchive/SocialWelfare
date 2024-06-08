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
  serviceList.map((item, index) => {
    $("#tableBody").append(`
        <tr>
            <th scope="row">${index + 1}</th>
            <td>${item.serviceName}</td>
            <td>${item.department}</td>
            <td><button class="btn btn-dark w-100" onclick='OpenForm(${
              item.serviceId
            })'>View</button></td>
        </tr>
    `);
  });
});
