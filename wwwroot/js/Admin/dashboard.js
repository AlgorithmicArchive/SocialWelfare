$(document).ready(function () {
  const count = countList;
  $("#services").text(count.serviceCount);
  $("#officers").text(count.officerCount);
  $("#citizens").text(count.citizenCount);
  $("#applications").text(count.applicationCount);
});
