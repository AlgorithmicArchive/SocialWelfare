$(document).ready(function () {
  
  const count = countList;
  const districtCode = count.districtCode;
  const conditions = {};
  const mappings = [
    { id: "#services", value: count.serviceCount },
    { id: "#officers", value: count.officerCount },
    { id: "#citizens", value: count.citizenCount },
    { id: "#applications", value: count.applicationCount },
    { id: "#total", value: count.totalCount },
    { id: "#pending", value: count.pendingCount },
    { id: "#rejected", value: count.rejectCount },
    { id: "#sanction", value: count.sanctionCount },
  ];

  SetServices();
  SetDistricts(districtCode);
  SetDesinations();

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
