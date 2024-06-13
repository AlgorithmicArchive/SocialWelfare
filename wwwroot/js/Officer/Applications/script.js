$(document).ready(function () {
  const Applications = list;
  console.log(Applications);
  if (Applications.Type == "Pending") {
    PendingTable(Applications);
    PoolTable(Applications);
  } else if (Applications.Type == "Sent") SentTable(Applications);
  else if (Applications.Type == "Sanction") MiscellaneousTable(Applications);

  let poolApplications = Applications.PoolList;
  let poolIdList = [];

  if (poolApplications && poolApplications.length > 0) {
    poolApplications.forEach((element) => {
      poolIdList.push(element.applicationId);
    });
    $("#containerSwitcher").show();
  }
  $(document).on("change", ".pool", function () {
    const currentVal = $(this).val();
    console.log(currentVal);
    if ($(this).is(":checked")) {
      if (!poolIdList.includes(currentVal)) {
        poolIdList.push(currentVal);
      }
    } else {
      poolIdList = poolIdList.filter((item) => item !== currentVal);
    }
    console.log(poolIdList);

    const transferButton = $("#mainContainer").find(
      "button:contains('Transfer To Pool')"
    );

    if (poolIdList.length > 0) {
      if (transferButton.length === 0) {
        $("#mainContainer").append(
          `<button class="btn btn-dark d-flex mx-auto" id="transferToPoolButton">
               Transfer To Pool
          </button>`
        );
      }
    } else {
      transferButton.remove();
    }
  });

  $(document).on("change", ".parent-pool", function () {
    const isChecked = $(this).is(":checked");
    $(".pool").prop("checked", isChecked).trigger("change");
  });

  $(document).on("click", "#transferToPoolButton", function () {
    Applications.PendingList = Applications.PendingList.filter((element) => {
      if (poolIdList.includes(element.applicationId)) {
        Applications.PoolList.push(element);
        return false; // Exclude this element from the new PendingList
      }
      return true; // Keep this element in the new PendingList
    });
    $("#containerSwitcher").show();
    const formData = new FormData();
    formData.append("serviceId", Applications.ServiceId);
    formData.append("poolIdList", JSON.stringify(poolIdList));
    console.log(Applications.ServiceId, poolIdList);
    fetch("/Officer/UpdatePool", { method: "post", body: formData })
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          console.log(data);
        }
      });
    PendingTable(Applications);
    PoolTable(Applications);
  });

  $("#sanctionAll").on("click", () =>
    SanctionAll(poolIdList, Applications.ServiceId)
  );

  $("#poolButton").on("click", function () {
    $("#mainContainer").hide();
    $("#PoolContainer").show();
    $("#containerSwitcher")
      .find(".btn-dark")
      .removeClass("btn-dark")
      .addClass("btn-secondary");

    $(this).removeClass("btn-secondary").addClass("btn-dark");
  });

  $("#mainButton").on("click", function () {
    $("#mainContainer").show();
    $("#PoolContainer").hide();
    $("#containerSwitcher")
      .find(".btn-dark")
      .removeClass("btn-dark")
      .addClass("btn-secondary");

    $(this).removeClass("btn-secondary").addClass("btn-dark");
  });
});
