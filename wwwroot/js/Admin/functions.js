const isEmpty = (obj) => {
  return Object.keys(obj).length === 0;
};

function createChart(labels, data) {
  const options = {
    labels: labels,
    datasets: [
      {
        label: "Applications",
        data: data,
        backgroundColor: ["blue", "darkgreen", "orange", "#0DCAF0", "red"],
        borderColor: ["blue", "darkgreen", "orange", "#0DCAF0", "red"],
        borderWidth: 1,
      },
    ],
  };
  // Config for the bar chart
  const config = {
    type: "bar",
    data: options,
    options: {
      responsive: false,
      plugins: {
        legend: {
          position: "top",
        },
        tooltip: {
          enabled: true,
        },
        datalabels: {
          color: "#fff",
          anchor: "end",
          align: "top",
          backgroundColor: (context) => {
            const index = context.dataIndex;
            const backgroundColors = [
              "blue",
              "darkgreen",
              "orange",
              "#0DCAF0",
              "red",
            ];
            return backgroundColors[index];
          },
          borderRadius: 4,
          padding: 6,
          formatter: (value) => {
            return value;
          },
        },
      },
      scales: {
        y: {
          beginAtZero: true,
        },
      },
    },
    plugins: [ChartDataLabels],
  };

  if (myChart) {
    myChart.destroy();
  }

  // Create new chart instance
  myChart = new Chart(document.getElementById("myChart"), config);
}

function createPieChart(labels, values) {
  const data = {
    labels: labels,
    datasets: [
      {
        label: "All Districts",
        data: values,
        backgroundColor: ["darkgreen", "orange", "#0DCAF0", "red"],
        borderColor: ["darkgreen", "orange", "#0DCAF0", "red"],
        borderWidth: 1,
      },
    ],
  };
  // Config for the doughnut chart
  const config = {
    type: "doughnut",
    data: data,
    options: {
      responsive: false,
      plugins: {
        legend: {
          position: "top",
        },
        tooltip: {
          enabled: true,
          backgroundColor: "#000", // Tooltip background color
          titleColor: "#ffffff", // Tooltip title color
          bodyColor: "#ffffff", // Tooltip body color
          callbacks: {
            label: function (tooltipItem) {
              return tooltipItem.label + ": " + tooltipItem.raw;
            },
          },
        },
        datalabels: {
          color: (context) => {
            const index = context.dataIndex;
            const colors = ["darkgreen", "orange", "#0DCAF0", "maroon"];
            return colors[index];
          },
          anchor: "center",
          align: "center",
          backgroundColor: (context) => {
            const index = context.dataIndex;
            const backgroundColors = ["white", "black", "black", "white"];
            return backgroundColors[index];
          },
          borderRadius: 4,
          padding: 5,
          font: {
            weight: "bold",
          },
          formatter: (value, ctx) => {
            let sum = 0;
            const dataArr = ctx.chart.data.datasets[0].data;
            dataArr.map((data) => {
              sum += data;
            });
            const percentage = ((value * 100) / sum).toFixed(2) + "%";
            return percentage;
          },
        },
      },
    },
    plugins: [ChartDataLabels],
  };
  const doughnutChart = new Chart(
    document.getElementById("allDistrict"),
    config
  );
}

function updateConditions(conditions) {
  const districtValue = $("#district").val();
  const officerValue = $("#officer").val();
  const serviceValue = $("#service").val();

  if (districtValue != "") {
    conditions["JSON_VALUE(a.ServiceSpecific, '$.District')"] = districtValue;
  } else {
    delete conditions["JSON_VALUE(a.ServiceSpecific, '$.District')"];
  }

  if (officerValue != "") {
    conditions["JSON_VALUE(app.value, '$.Officer')"] = officerValue;
  } else {
    delete conditions["JSON_VALUE(app.value, '$.Officer')"];
  }

  if (serviceValue != "") {
    conditions["a.ServiceId"] = serviceValue;
  } else {
    delete conditions["a.ServiceId"];
  }

  // if (!isEmpty(conditions)) {
  fetch("/Admin/GetFilteredCount?conditions=" + JSON.stringify(conditions))
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        $("#total").text(data.totalCount.length);
        $("#pending").text(data.pendingCount.length);
        $("#pendingWithCitizen").text(data.pendingWithCitizenCount.length);
        $("#rejected").text(data.rejectCount.length);
        $("#approved").text(data.sanctionCount.length);

        let isSanction = true;

        if (
          officerValue != "" &&
          data.forwardCount.length > 0 &&
          data.sanctionCount.length == 0
        ) {
          $("#approved").parent().find(".text").text("Forwarded");
          $("#approved").text(data.forwardCount.length);
          isSanction = false;
        } else if (
          officerValue != "" &&
          data.forwardCount.length == 0 &&
          data.sanctionCount.length > 0
        ) {
          $("#approved").parent().find(".text").text("Sanctioned");
          $("#approved").text(data.sanctionCount.length);
          isSanction = true;
        }

        createChart(
          [
            "Total",
            isSanction ? "Sanctioned" : "Forwarded",
            "Pending",
            "Pending With Citizen",
            "Rejected",
          ],
          [
            data.totalCount.length,
            isSanction ? data.sanctionCount.length : data.forwardCount.length,
            data.pendingCount.length,
            data.pendingWithCitizenCount.length,
            data.rejectCount.length,
          ]
        );
        $("#chartService").text($("#service option:selected").text());
        $("#chartOfficer").text(officerValue);
        $("#chartDistrict").text($("#district option:selected").text());
      }
    });
  // }
}

function SetDistricts(divisionCode) {
  fetch("/Admin/GetDistricts?division=" + divisionCode)
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        const districts = data.districts;
        let list = ``;
        districts.map((item) => {
          list += `<option value="${item.districtId}">${item.districtName}</option>`;
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
        $("#service").append(list);
      }
    });
}

function setApplicationList(applicationList) {
  const container = $("#applicationListContainer");
  container.empty();
  $("#dataGrid").removeClass("d-none").addClass("d-flex");
  initializeDataTable(
    "applicationListTable",
    "applicationListContainer",
    applicationList
  );
}
