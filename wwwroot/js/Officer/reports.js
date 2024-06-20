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
        backgroundColor: ["blue", "orange", "red", "darkgreen"],
        borderColor: ["blue", "orange", "red", "darkgreen"],
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
            const backgroundColors = ["blue", "orange", "red", "darkgreen"];
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

function updateConditions(conditions, count) {
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
    fetch("/Officer/GetFilteredCount?conditions=" + JSON.stringify(conditions))
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          $("#total").text(data.totalCount.length);
          $("#pending").text(data.pendingCount.length);
          $("#rejected").text(data.rejectCount.length);
          $("#sanction").text(data.sanctionCount.length);
          count.totalCount = data.totalCount;
          count.pendingCount = data.pendingCount;
          count.rejectCount = data.rejectCount;
          count.sanctionCount = data.sanctionCount;
          createChart(
            ["Total", "Pending", "Rejected", "Sanctioned"],
            [
              data.totalCount.length,
              data.pendingCount.length,
              data.rejectCount.length,
              data.sanctionCount.length,
            ]
          );
          $("#chartService").text($("#service option:selected").text());
          $("#chartOfficer").text(officerValue);
          $("#chartDistrict").text($("#district option:selected").text());
        }
      });
  }
}

function SetDistricts(districtCode) {
  fetch("/Base/GetDistricts")
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        console.log(data);
        const districts = data.districts;
        let districtName = "";
        districts.map((item) => {
          if (item.uuid == districtCode) districtName = item.districtName;
        });
        $("#district").text(districtName);
      }
    });
}

function SetDesinations(officer) {
  fetch("/Base/GetDesignations")
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        const designations = data.designations;
        let list = ``;
        designations.map((item) => {
          list += `<option value="${item.designation}">${item.designation}</option>`;
        });
        $("#officer").text(officer);
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

function setApplicationList(applicationList) {
  const container = $("#applicationListContainer");
  container.empty();
  applicationList.map((item, index) => {
    container.append(`
      <tr>
        <td>${index + 1}</td>
        <td>${item.applicationNo}</td>
        <td>${item.applicantName}</td>
        <td>${item.applicationStatus}</td>
        <td>${item.appliedDistrict}</td>
        <td>${item.appliedService}</td>
        <td>${item.applicationWithOfficer}</td>
      </tr>
    `);
  });

  $("#dataGrid").removeClass("d-none").addClass("d-flex");
  $("html, body").animate(
    {
      scrollTop: $("#dataGrid").offset().top,
    },
    "slow"
  );
}

function printDiv(divId) {
  var content = $("#" + divId).html();

  // Desired window size
  var width = 1080;
  var height = 600;

  // Calculate the position for centering the window
  var left = (screen.width - width) / 2;
  var top = (screen.height - height) / 2;

  var myWindow = window.open(
    "",
    "",
    `width=${width},height=${height},top=${top},left=${left}`
  );
  myWindow.document.write("<html><head><title>Print</title>");
  myWindow.document.write(`<style>
        table { border-collapse: collapse; }
        th, td { border: 2px solid black; padding: 8px; text-align: left; }
        thead { background-color: #f2f2f2; }
        th { border: 1px solid black; }
        @media print {
            @page {
                size: landscape;
            }
        }
    </style>`);
  myWindow.document.write("</head><body>");
  myWindow.document.write(content);
  myWindow.document.write("</body></html>");
  myWindow.document.close();
  myWindow.focus();
  myWindow.print();
  myWindow.close();
}

$(document).ready(function () {
  const count = countList;
  const districtCode = count.districtCode;
  const officer = count.officer;
  const conditions = {};
  const mappings = [
    { id: "#services", value: count.serviceCount.length },
    { id: "#officers", value: count.officerCount.length },
    { id: "#citizens", value: count.citizenCount.length },
    { id: "#applications", value: count.applicationCount.length },
    { id: "#total", value: count.totalCount.length },
    { id: "#pending", value: count.pendingCount.length },
    { id: "#pendingWithCitizen", value: count.pendingWithCitizenCount.length },
    { id: "#rejected", value: count.rejectCount.length },
    { id: "#sanction", value: count.sanctionCount.length },
  ];
  const AllDistrictCount = count.allDistrictCount;
  createPieChart(
    ["Pending", "Rejected", "Sanctioned"],
    [
      AllDistrictCount.pending.length,
      AllDistrictCount.rejected.length,
      AllDistrictCount.sanctioned.length,
    ]
  );

  SetServices();
  SetDistricts(districtCode);
  SetDesinations(officer);

  mappings.forEach((mapping) => {
    $(mapping.id).text(mapping.value);
  });

  createChart(
    ["Total", "Pending", "Rejected", "Sanctioned"],
    [
      count.totalCount.length,
      count.pendingCount.length,
      count.rejectCount.length,
      count.sanctionCount.length,
    ]
  );

  $("#service").on("change", function () {
    updateConditions(conditions, count);
  });
  $("#district").on("change", function () {
    updateConditions(conditions);
  });
  $("#officer").on("change", function () {
    updateConditions(conditions);
  });

  $(".count-card").on("click", function () {
    const card = $(this).find(".text").text();
    let applicationList;
    if (card == "Total") applicationList = count.totalCount;
    else if (card == "Pending") applicationList = count.pendingCount;
    else if (card == "Sanctioned") applicationList = count.sanctionCount;
    else if (card == "Pending With Citizen")
      applicationList = count.pendingWithCitizenCount;
    else if (card == "Rejected") applicationList = count.rejectCount;

    setApplicationList(applicationList);
  });
});
