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
        backgroundColor: ["orange", "red", "darkgreen"],
        borderColor: ["orange", "red", "darkgreen"],
        borderWidth: 1,
      },
    ],
  };
  // Config for the bar chart
  const config = {
    type: "bar",
    data: options,
    options: {
      responsive: true,
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
            const backgroundColors = ["orange", "red", "darkgreen"];
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
        backgroundColor: ["orange", "red", "darkgreen"],
        borderColor: ["orange", "red", "darkgreen"],
        borderWidth: 1,
      },
    ],
  };
  // Config for the doughnut chart
  const config = {
    type: "doughnut",
    data: data,
    options: {
      responsive: true,
      plugins: {
        legend: {
          position: "top",
        },
        tooltip: {
          enabled: true,
        },
        datalabels: {
          color: "#fff",
          anchor: "center",
          align: "center",
          backgroundColor: (context) => {
            const index = context.dataIndex;
            const backgroundColors = ["maroon", "red", "purple"];
            return backgroundColors[index];
          },
          borderRadius: 4,
          padding: 5,
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
    fetch("/Base/GetFilteredCount?conditions=" + JSON.stringify(conditions))
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
          $("#chartService").text($("#service option:selected").text());
          $("#chartOfficer").text(officerValue);
        }
      });
  }
}

function SetDistricts(districtCode) {
  fetch("/Base/GetDistricts")
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        const districts = data.districts;
        let list = ``;
        districts.map((item) => {
          list += `<option value="${item.uuid}">${item.districtName}</option>`;
        });
        $("#district").append(list);
        $("#district").val(districtCode);
        $("#chartDistirct").text($("#district option:selected").text());
      }
    });
}

function SetDesinations(designation) {
  fetch("/Base/GetDesignations")
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        const designations = data.designations;
        let list = ``;
        designations.map((item) => {
          list += `<option value="${item.designation}">${item.designation}</option>`;
        });
        $("#officer").append(list);
        $("#officer").val(designation);
        $("#chartOfficer").text($("#officer option:selected").text());
      }
    });
}

function SetServices() {
  fetch("/Base/GetServices")
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

$(document).ready(function () {
  const count = countList;
  const districtCode = count.districtCode;
  const designation = count.officerDesignation;
  const conditions = {};
  const mappings = [
    { id: "#services", value: count.serviceCount },
    { id: "#officers", value: count.officerCount },
    { id: "#citizens", value: count.citizenCount },
    { id: "#applications", value: count.applicationCount },
    { id: "#total", value: count.totalCount },
    { id: "#pending", value: count.pendingCount },
    { id: "#pendingWithCitizen", value: count.pendingWithCitizenCount },
    { id: "#rejected", value: count.rejectCount },
    { id: "#sanction", value: count.sanctionCount },
  ];
  const AllDistrictCount = count.allDistrictCount;
  createPieChart(
    ["Pending", "Rejected", "Sanctioned"],
    [
      AllDistrictCount.pending,
      AllDistrictCount.rejected,
      AllDistrictCount.sanctioned,
    ]
  );

  SetServices();
  SetDistricts(districtCode);
  SetDesinations(designation);

  if (districtCode != undefined) $("#district").val(districtCode);

  mappings.forEach((mapping) => {
    $(mapping.id).text(mapping.value);
  });

  createChart(
    ["Pending", "Rejected", "Sanctioned"],
    [count.pendingCount, count.rejectCount, count.sanctionCount]
  );

  $("#service").on("change", function () {
    updateConditions(conditions);
  });
  $("#district").on("change", function () {
    updateConditions(conditions);
  });
  $("#officer").on("change", function () {
    updateConditions(conditions);
  });
});
