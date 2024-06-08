function createTableRow(cells) {
  const tr = $("<tr/>");
  cells.forEach((cell) => tr.append(cell));
  return tr;
}

function InsertDetailRow(
  applicationId,
  applicantName,
  index,
  isSentApplication,
  canSanction,
  canPull = null
) {
  const viewButton = `<td><button class="btn btn-dark w-100" onclick='ShowUserDetails("${applicationId}")'>View</button></td>`;
  const pullButton = `<td><button class="btn btn-dark w-100" onclick='PullApplication("${applicationId}")'>Pull</button></td>`;

  const cells = [
    `<th scope="row">${index}</th>`,
    `<td>${applicationId}</td>`,
    `<td>${applicantName}</td>`,
    isSentApplication
      ? canPull
        ? pullButton
        : `<td>Cannot Pull</td>`
      : viewButton,
  ];

  const container = $("#userDetails");

  if (canSanction) {
    const thead = container.closest("table").find("thead tr");
    if (thead.find("th:contains('Pool')").length === 0) {
      thead.prepend(
        `<th class="d-flex gap-2"><input class="form-check parent-pool" type="checkbox" />Pool</th>`
      );
    }
    cells.unshift(
      `<td><input class="form-check pool" type="checkbox" value="${applicationId}" /></td>`
    );
  }
  container.append(createTableRow(cells));
}

function InsertPoolRow(applicationId, applicantName, index) {
  const cells = [
    `<th scope="row">${index}</th>`,
    `<td>${applicationId}</td>`,
    `<td>${applicantName}</td>`,
  ];

  const container = $("#poolArray");

  // Check if the "Sanction All" button already exists
  if (
    container.closest("div").find("button:contains('Sanction All')").length ===
    0
  ) {
    container
      .closest("div")
      .append(
        `<button id="SanctionAll" class="d-flex btn btn-dark mx-auto">Sanction All</button>`
      );
  }

  container.append(createTableRow(cells));
}

function InsertUpdateRow(
  applicationId,
  applicantName,
  index,
  valueToUpdate,
  newValue
) {
  const cells = [
    `<th scope="row">${index}</th>`,
    `<td>${applicationId}</td>`,
    `<td>${applicantName}</td>`,
    `<td>${valueToUpdate}</td>`,
    `<td>${newValue}</td>`,
  ];

  const container = $("#updateList");
  container.append(createTableRow(cells));
}

function ShowUserDetails(applicationId) {
  $("#ApplicationId").val(applicationId);
  $("#UserDetails").submit();
}

function PullApplication(applicationId) {
  const formData = new FormData();
  formData.append("ApplicationId", applicationId);
  fetch("/Officer/PullApplication", { method: "post", body: formData })
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        window.location.href = "/Officer/Inbox";
      }
    });
}

function showContainer(first, second) {
  $("#sectionButtons").remove();
  $("#" + first).show();
  $("#" + second).hide();

  $("#" + first).prepend(`
    <div class="container center-align mb-3 gap-2" id="sectionButtons">
          <button class="btn btn-dark" onclick='showContainer("mainContainer","PoolContainer")'>Main Container</button>
          <button class="btn btn-dark"  onclick='showContainer("PoolContainer","mainContainer")'>Pool Conatiner</button>
    </div>
  `);
}

function MoveToPool(
  poolList,
  listArray,
  poolArray,
  serviceId,
  isSentApplication,
  canSanction
) {
  console.log(poolList);
  for (let i = listArray.length - 1; i >= 0; i--) {
    if (poolList.includes(listArray[i]["applicationId"])) {
      poolArray.push(listArray[i]);
      listArray.splice(i, 1);
    }
  }

  $("#userDetails").empty();

  listArray.length > 0
    ? listArray.forEach((item, index) => {
        InsertDetailRow(
          item.applicationId,
          item.applicantName,
          index + 1,
          isSentApplication,
          canSanction
        );
      })
    : $("#userDetails").append(
        `<td colspan="4" class="text-center fw-bold fs-3">NO RECORDS</td>`
      );

  $("#poolArray").empty();
  poolArray.forEach((item, index) => {
    InsertPoolRow(item.applicationId, item.applicantName, index + 1);
  });
  if ($("#sectionButtons").length === 0) {
    $("#mainContainer").prepend(`
      <div class="container center-align mb-3 gap-2" id="sectionButtons">
        <button class="btn btn-dark" onclick='showContainer("mainContainer","PoolContainer")'>Main Container</button>
        <button class="btn btn-dark" onclick='showContainer("PoolContainer","mainContainer")'>Pool Container</button>
      </div>
    `);
  }

  const formdata = new FormData();
  formdata.append("poolList", JSON.stringify(poolList));
  formdata.append("serviceId", serviceId);
  fetch("/Officer/UpdatePool", { method: "post", body: formdata })
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        console.log(data);
      }
    });
}

async function SanctionAll(Officer, poolList) {
  for (let i = 0; i < poolList.length; i++) {
    hideSpinner();
    const userApproved = confirm(
      `Do you want to issue sanction letter for Reference Number ${poolList[i]}?`
    );
    showSpinner();
    if (userApproved) {
      try {
        const formdata = new FormData();
        formdata.append("ApplicationId", poolList[i]);
        formdata.append("Officer", Officer);
        formdata.append("Action", "Sanction");
        formdata.append("Remarks", "Sanctioned");
        const response = await fetch("/Officer/Action", {
          method: "post",
          body: formdata,
        });
        const data = await response.json();
        if (data.status) {
          console.log(data.url);
        }
      } catch (error) {
        console.error("Error:", error);
      }
    } else {
      continue;
    }
  }
  const formdata = new FormData();
  formdata.append("poolList", JSON.stringify([]));
  formdata.append("serviceId", applications.serviceId);
  fetch("/Officer/UpdatePool", { method: "post", body: formdata })
    .then((res) => res.json())
    .then((data) => {
      hideSpinner();
      window.location.href = "/Officer/Inbox";
    });
}

$(document).ready(function () {
  const isSentApplication =
    window.location.pathname === "/Officer/SentApplications";
  const canSanction = applications.canSanction;
  const poolArray = applications.poolList;
  const listArray = applications.list;
  const updateArray = applications.updateList;
  const serviceId = applications.serviceId;
  const Officer = applications.officerDesignation;
  let poolList = [];
  if (canSanction && poolArray != "" && poolArray.length != 0) {
    poolArray.forEach((item, index) => {
      poolList.push(item.applicationId);
      InsertPoolRow(item.applicationId, item.applicantName, index + 1);
    });
    $("#mainContainer").prepend(`
        <div class="container center-align mb-3 gap-2" id="sectionButtons">
          <button class="btn btn-dark" onclick='showContainer("mainContainer","PoolContainer")'>Main Container</button>
          <button class="btn btn-dark"  onclick='showContainer("PoolContainer","mainContainer")'>Pool Conatiner</button>
        </div>
    `);
  }

  $(document).on("change", ".pool", function () {
    const currentVal = $(this).val();

    // Add only if the item is not already in the array
    if ($(this).is(":checked")) {
      if (!poolList.includes(currentVal)) {
        poolList.push(currentVal);
      }
    } else {
      // Remove the item if it is unchecked
      poolList = poolList.filter((item) => item !== currentVal);
    }

    // Check if Transfer to Pool button exists
    const transferButton = $("#mainContainer").find(
      "button:contains('Transfer To Pool')"
    );

    if (poolList.length > 0) {
      // Append Transfer to Pool button if it doesn't exist
      if (transferButton.length === 0) {
        $("#mainContainer").append(`
                <button class="btn btn-dark mx-auto" id="transferToPoolButton">Transfer To Pool</button>
            `);
      }
      // Update the button's onclick attribute with the latest poolList
      $("#transferToPoolButton").attr(
        "onclick",
        `MoveToPool(${JSON.stringify(poolList)}, ${JSON.stringify(
          listArray
        )}, ${JSON.stringify(
          poolArray
        )}, ${serviceId}, ${isSentApplication}, ${canSanction})`
      );
    } else {
      transferButton.remove();
    }
  });

  $(document).on("change", ".parent-pool", function () {
    const isChecked = $(this).is(":checked");
    $(".pool").prop("checked", isChecked).trigger("change");
  });

  $(document).on("click", "#SanctionAll", function () {
    SanctionAll(Officer, poolList);
  });

  listArray.length > 0
    ? listArray.forEach((item, index) => {
        InsertDetailRow(
          item.applicationId,
          item.applicantName,
          index + 1,
          isSentApplication,
          canSanction,
          item.canPull
        );
      })
    : $("#userDetails").append(
        `<td colspan="4" class="text-center fw-bold fs-3">NO RECORDS</td>`
      );

  if (updateArray && updateArray.length > 0) {
    updateArray.forEach((item, index) => {
      const updateRequest = JSON.parse(item.updateRequest);
      InsertUpdateRow(
        item.applicationId,
        item.applicantName,
        index + 1,
        updateRequest.formElement.label,
        updateRequest.newValue
      );
    });
  } else $("#updateList").parent().parent().hide();
});
