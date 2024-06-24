$(document).ready(function () {
  const count = countList;
  console.log(count);
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
    ["Sanctioned","Pending","Pending With Citizen","Rejected"],
    [
      AllDistrictCount.sanctioned.length,
      AllDistrictCount.pending.length,
      AllDistrictCount.pendingWithCitizen.length,
      AllDistrictCount.rejected.length,
    ]
  );

  SetServices();
  SetDistricts(divisionCode);
  SetDesinations();

  mappings.forEach((mapping) => {
    $(mapping.id).text(mapping.value);
  });

  createChart(
    ["Total", "Sanctioned","Pending","Pending With Citizen", "Rejected"],
    [
      count.totalCount.length,
      count.sanctionCount.length,
      count.pendingCount.length,
      count.pendingWithCitizenCount.length,
      count.rejectCount.length,
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
    const card = $(this).attr("id");
    console.log(card);
    let applicationList;
    if (card == "Total") applicationList = count.totalCount;
    else if (card == "Pending") applicationList = count.pendingCount;
    else if (card == "Sanctioned") applicationList = count.sanctionCount;
    else if (card == "PendingWithCitizen")
      applicationList = count.pendingWithCitizenCount;
    else if (card == "Rejected") applicationList = count.rejectCount;

    setApplicationList(applicationList);
    var $anchor = $("<a/>", {
      href: "#dataGrid",
      id: "tempAnchor",
    }).appendTo("body");

    // Trigger the click event on the anchor tag
    $anchor[0].click();

    // Clean up by removing the anchor tag after clicking
    $anchor.remove();
  });
});
