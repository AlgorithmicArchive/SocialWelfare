$(document).ready(function () {
  const count = countList;
  const divisionCode = count.divisionCode;
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
  SetDistricts(divisionCode);
  SetDesinations();

  mappings.forEach((mapping) => {
    $(mapping.id).text(mapping.value);
  });

  createChart(
    ["Pending", "Rejected", "Sanctioned"],
    [
      count.pendingCount.length,
      count.rejectCount.length,
      count.sanctionCount.length,
    ]
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

  $(".count-card").on("click", function () {
    const card = $(this).find(".text").text();
    let applicationList;
    if (card == "Total") applicationList = count.totalCount;
    else if (card == "Pending") applicationList = count.pendingCount;
    else if (card == "Sanctioned") applicationList = count.sanctionCount;
    else if (card == "With Citizen")
      applicationList = count.pendingWithCitizenCount;
    else if (card == "Rejected") applicationList = count.rejectCount;

    setApplicationList(applicationList);
    $("#applicationList").modal("show");
  });
});
