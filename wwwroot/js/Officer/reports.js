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
function updateConditions(count) {
  const serviceValue = $("#service").val();

  if (serviceValue != "") {
    fetch("/Officer/GetFilteredCount?serviceId=" + parseInt(serviceValue))
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          console.log(data, count);
          $("#total").text(data.countList.totalCount);
          $("#pending").text(data.countList.pendingCount);
          $("#rejected").text(data.countList.rejectCount);
          $("#sanction").text(data.countList.sanctionCount);
          count.totalCount = data.countList.totalCount;
          count.pendingCount = data.countList.pendingCount;
          count.rejectCount = data.countList.rejectCount;
          count.sanctionCount = data.countList.sanctionCount;
          createChart(
            [
              "Total",
              "Sanctioned",
              "Pending",
              "Pending With Citizen",
              "Rejected",
            ],
            [
              count.totalCount,
              count.sanctionCount,
              count.pendingCount,
              count.pendingWithCitizenCount,
              count.rejectCount,
            ]
          );
          $("#chartService").text($("#service option:selected").text());
          $("#chartOfficer").text($("#officer").text());
          $("#chartDistrict").text($("#distric").text());
        }
      });
  }
}
function SetDistricts(districtCode) {
  console.log(districtCode);
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
        if (districtCode == 0) $("#district").text("All");
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
function setApplicationList(totalCount, type) {
  type = type == "PendingWithCitizen" ? "ReturnToEdit" : type;
  console.log(totalCount, type);
  let serviceId = $("#service").val();
  let officer = $("#officer").val();
  let district = $("#district").val();
  if (serviceId == "") serviceId = 0;
  if (district == "") district = 0;

  if (totalCount != 0) {
    initializeDataTable(
      "applicationsTable",
      "/Officer/GetTableRecords",
      serviceId,
      officer,
      district,
      type,
      totalCount,
      0,
      10
    );
  }
  $("html, body").animate(
    {
      scrollTop: $("#applicationsTable").offset().top,
    },
    "slow"
  );
}
let applicationList;

$(document).ready(function () {
  const count = obj.countList;
  console.log(count);
  const districtCode = obj.districtCode;
  const officer = obj.designation;
  const mappings = [
    { id: "#total", value: count.totalCount },
    { id: "#pending", value: count.pendingCount },
    { id: "#pendingWithCitizen", value: count.pendingWithCitizenCount },
    { id: "#rejected", value: count.rejectCount },
    { id: "#sanction", value: count.sanctionCount },
  ];
  const AllDistrictCount = obj.allDistrictCount;

  createChart(
    ["Total", "Sanctioned", "Pending", "Pending With Citizen", "Rejected"],
    [
      count.totalCount,
      count.sanctionCount,
      count.pendingCount,
      count.pendingWithCitizenCount,
      count.rejectCount,
    ]
  );
  createPieChart(
    ["Sanctioned", "Pending", "Pending With Citizen", "Rejected"],
    [
      AllDistrictCount.sanctionCount,
      AllDistrictCount.pendingCount,
      AllDistrictCount.pendingWithCitizenCount,
      AllDistrictCount.rejectedCount,
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
      count.totalCount,
      count.sanctionCount,
      count.pendingCount,
      count.pendingWithCitizenCount,
      count.rejectCount,
    ]
  );

  $("#service").on("change", function () {
    updateConditions(count);
  });

  $(".dashboard-card").on("click", function () {
    const card = $(this).attr("id");
    if (card == "Total") applicationList = count.totalCount;
    else if (card == "Pending") applicationList = count.pendingCount;
    else if (card == "Sanctioned") applicationList = count.sanctionCount;
    else if (card == "PendingWithCitizen")
      applicationList = count.pendingWithCitizenCount;
    else if (card == "Rejected") applicationList = count.rejectCount;

    setApplicationList(applicationList, card);
  });

  $("#getRecords").on("click", function () {
    setApplicationList(applicationList);
  });
});
