$(document).ready(function () {
  const count = data.countList;
  const AllDistrictCount = data.allDistrictCount;
  const divisionCode = count.divisionCode;
  let applicationList;
  const conditions = {};

  const mappings = [
    { id: "#total", value: count.totalCount },
    { id: "#pending", value: count.pendingCount },
    { id: "#pendingWithCitizen", value: count.pendingWithCitizenCount },
    { id: "#rejected", value: count.rejectCount },
    { id: "#approved", value: count.sanctionCount },
  ];
  createPieChart(
    ["Sanctioned", "Pending", "Pending With Citizen", "Rejected"],
    [
      AllDistrictCount.sanctionCount,
      AllDistrictCount.pendingCount,
      AllDistrictCount.pendingWithCitizenCount,
      AllDistrictCount.rejectCount,
    ]
  );

  SetServices();
  SetDistricts(divisionCode);
  SetDesinations();

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

  $("#service").change(function () {
    updateConditions(conditions);
  });

  $("#getRecords").on("click", function () {
    setApplicationList(applicationList);
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

    var $anchor = $("<a/>", {
      href: "#applicationsTable",
      id: "tempAnchor",
    }).appendTo("body");

    // Trigger the click event on the anchor tag
    $anchor[0].click();

    // Clean up by removing the anchor tag after clicking
    $anchor.remove();
  });
});
