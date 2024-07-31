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
        backgroundColor: ["darkgreen", "orange", "#0DCAF0", "maroon"],
        borderColor: ["darkgreen", "orange", "#0DCAF0", "maroon"],
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
        },
        datalabels: {
          color: "#fff",
          anchor: "center",
          align: "center",
          backgroundColor: (context) => {
            const index = context.dataIndex;
            const backgroundColors = ["black", "black", "black"];
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
  const districtValue = $("#district").attr("data-id");
  const officerValue = $("#officer").text();
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
            [
              "Total",
              "Sanctioned",
              "Pending",
              "Pending With Citizen",
              "Rejected",
            ],
            [
              count.totalCount.length,
              count.sanctionCount.length,
              count.pendingCount.length,
              count.pendingWithCitizenCount.length,
              count.rejectCount.length,
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
        const districts = data.districts;
        let districtName = "";
        districts.map((item) => {
          if (item.districtId == districtCode) districtName = item.districtName;
        });
        $("#district").text(districtName);
        $("#district").attr("data-id", districtCode);
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
function convertCamelCaseToReadable(str) {
  return (
    str
      // Insert a space before all capital letters
      .replace(/([A-Z])/g, " $1")
      // Capitalize the first letter of the resulting string
      .replace(/^./, function (char) {
        return char.toUpperCase();
      })
  );
}
function setApplicationList(applicationList) {
  let propertiesToRemove = [];

  let checkedValues = $('input[name="chosenColumns"]:checked')
    .map(function () {
      return this.value;
    })
    .get();

  console.log(checkedValues);
  if (checkedValues.length > 0) {
    let properties = Object.keys(applicationList[0]);
    properties.forEach((item) => {
      if (!checkedValues.includes(item)) propertiesToRemove.push(item);
    });
    $("#applicationListTable thead tr").empty();

    console.log($("#applicationListTable thead tr"));
    // $("#applicationListTable thead tr").append(`<th>S.No.</th>`);

    // checkedValues.map((item) =>
    //   $("#applicationListTable thead tr").append(
    //     `<th>${convertCamelCaseToReadable(item)}</th>`
    //   )
    // );
    $('input[name="chosenColumns"]').prop("checked", false);
  } else {
    $("#applicationListTable thead tr").empty();
    $("#applicationListTable thead tr").append(`
        <th>S.No.</th>
        <th>Application Number</th>
        <th>Applicant Name</th>
        <th>Application Status</th>
        <th>Applied District</th>
        <th>Applied Service</th>
        <th>Application Currently With</th>
        <th>Application Received On</th>
        <th>Application Submission Date</th>
      `);
  }

  applicationList = applicationList.map((obj) => {
    return propertiesToRemove.reduce((acc, property) => {
      const { [property]: _, ...rest } = acc;
      return rest;
    }, obj);
  });

  // Update the table body with the new data
  const container = $("#applicationListContainer");
  container.empty();
  $("#dataGrid").removeClass("d-none").addClass("d-flex");

  // applicationList.forEach((item, index) => {
  //   let row = `<tr><td>${index + 1}</td>`; // First column for index

  //   for (const key in item) {
  //     if (item.hasOwnProperty(key)) {
  //       row += `<td>${item[key] == "" ? "N/A" : item[key]}</td>`;
  //     }
  //   }

  //   row += `</tr>`;
  //   container.append(row); // Append each row to the container
  // });

  // Initialize DataTable after the table structure is complete
  initializeDataTable(
    "applicationListTable",
    "applicationListContainer",
    applicationList
  );

  $("html, body").animate(
    {
      scrollTop: $("#dataGrid").offset().top,
    },
    "slow"
  );
}
let applicationList;

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

  createChart(
    ["Total", "Sanctioned", "Pending", "Pending With Citizen", "Rejected"],
    [
      count.totalCount.length,
      count.sanctionCount.length,
      count.pendingCount.length,
      count.pendingWithCitizenCount.length,
      count.rejectCount.length,
    ]
  );
  createPieChart(
    ["Sanctioned", "Pending", "Pending With Citizen", "Rejected"],
    [
      AllDistrictCount.sanctioned.length,
      AllDistrictCount.pending.length,
      AllDistrictCount.pendingWithCitizen.length,
      AllDistrictCount.rejected.length,
    ]
  );

  SetServices();
  SetDistricts(districtCode);
  SetDesinations(officer);

  mappings.forEach((mapping) => {
    $(mapping.id).text(mapping.value);
  });

  createChart(
    ["Total", "Sanctioned", "Pending", "Pending With Citizen", "Rejected"],
    [
      count.totalCount.length,
      count.sanctionCount.length,
      count.pendingCount.length,
      count.pendingWithCitizenCount.length,
      count.rejectCount.length,
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

  $(".dashboard-card").on("click", function () {
    const card = $(this).attr("id");
    if (card == "Total") applicationList = count.totalCount;
    else if (card == "Pending") applicationList = count.pendingCount;
    else if (card == "Sanctioned") applicationList = count.sanctionCount;
    else if (card == "PendingWithCitizen")
      applicationList = count.pendingWithCitizenCount;
    else if (card == "Rejected") applicationList = count.rejectCount;

    setApplicationList(applicationList);
  });

  $("#getRecords").on("click", function () {
    setApplicationList(applicationList);
  });
});
