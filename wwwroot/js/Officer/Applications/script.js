let Applications;
let poolApplications;
let poolIdList = [];
let finalList = [];

function onSelect(list) {
  Applications = list;
  console.log(Applications);
  if (Applications.Type == "Pending") {
    PendingTable(Applications);
    PoolTable(Applications);
  } else if (Applications.Type == "Sent") SentTable(Applications);
  else if (Applications.Type == "Sanction") MiscellaneousTable(Applications);

  $("#listTable").show();

  poolApplications = Applications.PoolList;
  if (poolApplications && poolApplications.length > 0) {
    poolApplications.forEach((element) => {
      poolIdList.push(element.applicationId);
    });
    $("#containerSwitcher").show();
  }
}

$(document).ready(function () {
  $(document).on("change", ".pool", function () {
    const currentVal = $(this).val();
    if ($(this).is(":checked")) {
      if (!poolIdList.includes(currentVal)) {
        poolIdList.push(currentVal);
      }
    } else {
      poolIdList = poolIdList.filter((item) => item !== currentVal);
    }
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

  $(document).on("change", ".poolList-element", function () {
    const currentVal = $(this).val();
    if ($(this).is(":checked")) {
      if (!finalList.includes(currentVal)) {
        finalList.push(currentVal);
      }
    } else {
      finalList = finalList.filter((item) => item !== currentVal);
    }

    if (finalList.length > 0) {
      $("#sanctionAll").prop("disabled", false);
      $("#transferBack").prop("disabled", false);
    } else {
      $("#sanctionAll").prop("disabled", true);
      $("#transferBack").prop("disabled", true);
    }
  });

  $(document).on("change", ".parent-pool", function () {
    const isChecked = $(this).is(":checked");
    $(".pool").prop("checked", isChecked).trigger("change");
  });

  $(document).on("change", ".poolList-parent", function () {
    const isChecked = $(this).is(":checked");
    $(".poolList-element").prop("checked", isChecked).trigger("change");
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
    formData.append("poolAction", "add");
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

  $(document).on("click", "#transferBack", function () {
    Applications.PoolList = Applications.PoolList.filter((element) => {
      if (finalList.includes(element.applicationId)) {
        Applications.PendingList.push(element);
        return false; // Exclude this element from the new PoolList
      }
      return true; // Keep this element in the new PoolList
    });
    $("#containerSwitcher").show();
    const formData = new FormData();
    formData.append("serviceId", Applications.ServiceId);
    formData.append("poolIdList", JSON.stringify(finalList));
    formData.append("poolAction", "remove");
    console.log(Applications.ServiceId, finalList);
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
    SanctionAll(poolIdList, finalList, Applications.ServiceId)
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
