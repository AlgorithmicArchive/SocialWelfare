const isEmpty = (obj) => {
  return Object.keys(obj).length === 0;
};

function createChart(labels, data) {
  const config = {
    type: "line",
    data: {
      labels: labels,
      datasets: [
        {
          label: "Applications",
          backgroundColor: "rgba(255, 99, 132, 0.2)",
          borderColor: "rgba(255, 99, 132, 1)",
          borderWidth: 1,
          data: data,
        },
      ],
    },
    options: {
      scales: {
        y: {
          beginAtZero: true,
        },
      },
    },
  };

  if (myChart) {
    myChart.destroy();
  }

  // Create new chart instance
  myChart = new Chart(document.getElementById("myChart"), config);
}

function updateConditions(conditions) {
  const districtValue = $("#district").val();
  const officerValue = $("#officer").val();
  const serviceValue = $("#service").val();

  if (districtValue != "") {
    conditions["specific.District"] = districtValue;
  } else {
    delete conditions["specific.District"];
  }

  if (officerValue != "") {
    conditions["JSON_VALUE(app.value, '$.Officer')"] = officerValue;
  } else {
    delete conditions["JSON_VALUE(app.value, '$.Officer')"];
  }

  if (serviceValue != "") {
    conditions["application.ServiceId"] = serviceValue;
  } else {
    delete conditions["application.ServiceId"];
  }

  if (!isEmpty(conditions)) {
    fetch("/Admin/GetFilteredCount?conditions=" + JSON.stringify(conditions))
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          $("#total").text(data.totalCount);
          $("#pending").text(data.pendingCount);
          $("#rejected").text(data.rejectCount);
          $("#sanction").text(data.sanctionCount);
          createChart(
            ["Pending", "Rejected", "Sanctioned"],
            [data.pendingCount, data.rejectCount, data.sanctionCount]
          );
        }
      });
  }
}

function SetDistricts() {
  fetch("/Admin/GetDistricts")
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        const districts = data.districts;
        let list = ``;
        districts.map((item) => {
          list += `<option value="${item.uuid}">${item.districtName}</option>`;
        });
        $("#district").append(list);
      }
    });
}

function SetDesinations() {
  fetch("/Admin/GetDesignations")
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        const designations = data.designations;
        let list = ``;
        designations.map((item) => {
          list += `<option value="${item.designation}">${item.designation}</option>`;
        });
        $("#officer").append(list);
      }
    });
}

function SetServices() {
  fetch("/Admin/GetServices")
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        const services = data.services;
        let list = ``;
        services.map((item) => {
          list += `<option value="${item.serviceId}">${item.serviceName}</option>`;
        });
        console.log(list);
        $("#service").append(list);
      }
    });
}
