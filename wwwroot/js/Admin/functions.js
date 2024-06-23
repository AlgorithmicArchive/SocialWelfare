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
        backgroundColor: ["blue","darkgreen", "orange","lightblue", "red"],
        borderColor: ["blue","darkgreen", "orange","lightblue", "red"],
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
            const backgroundColors = ["blue","darkgreen", "orange","lightblue", "red"];
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
            const backgroundColors = ["blac", "black", "black"];
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

  if (!isEmpty(conditions)) {
    fetch("/Admin/GetFilteredCount?conditions=" + JSON.stringify(conditions))
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          console.log(data);
          $("#total").text(data.totalCount.length);
          $("#pending").text(data.pendingCount.length);
          $("#rejected").text(data.rejectCount.length);
          $("#sanction").text(data.sanctionCount.length);
          createChart(
            ["Total", "Sanctioned","Pending","Pending With Citizen", "Rejected"],
            [
              data.totalCount.length,
              data.sanctionCount.length,
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
  }
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

  console.log("This one only");
  $("#dataGrid").removeClass("d-none").addClass("d-flex");
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
