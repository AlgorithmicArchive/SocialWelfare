$(document).ready(function () {
  function element(item, index, reverse) {
    return `<div class="bg-primary flex-column w-25 rounded-border" id="${index}" style="cursor:pointer;${reverse ? "" : ""}">
            <p class="text-white w-100 text-center fs-6 fw-bold py-2">${
              item.actionTaker
            }</p>
            <p class="text-white w-100 text-center fs-6 fw-bold py-2">${
              item.actionTaken
            }</p>
            <p class="text-white w-100 text-center fs-6 fw-bold py-2">${
              item.dateTime
            }</p>
           </div>`;
  }

  function show() {
    var $container = $("#displayList");
    var length = arr.length;
    var rows = Math.ceil(length / 3);
    let arrowEnd = true;
    for (var i = 0; i < rows; i++) {
      var $row = $(
        '<div class="row d-flex align-items-center justify-content-between"></div>'
      );
      if (i % 2 === 0) {
        // Normal direction
        for (var j = i * 3; j < Math.min((i + 1) * 3, length); j++) {
          $row.append(element(arr[j], j, false));
          if (j < Math.min((i + 1) * 3, length) - 1) {
            $row.append(
              '<i class="fa-solid fs-1 fa-arrow-right" style="width:0"></i>'
            );
          }
        }
      } else {
        // Reverse direction
        for (var j = Math.min((i + 1) * 3, length) - 1; j >= i * 3; j--) {
          $row.append(element(arr[j], j, true));
          if (j > i * 3) {
            $row.append(
              '<i class="fa-solid fs-1 fa-arrow-left" style="width:0"></i>'
            );
          }
        }
      }
      $container.append($row);
      if (i < rows - 1) {
        $container.append(
          `<div class="row"><div class="${
            arrowEnd ? "offset-md-9" : ""
          } col-md-3  d-flex justify-content-center"><span ><i class="fa-solid fs-1 fa-arrow-down"></i></span></div></div>`
        );
        arrowEnd = !arrowEnd;
      }
    }
  }
  var arr = [];
  $("#getFlow").click(function () {
    const applicationId = $("#applicationId").val();
    console.log(applicationId);
    fetch("/Admin/GetHistory?applicationId=" + applicationId)
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          const history = JSON.parse(data.result.history);
          history.forEach((item) => {
            arr.push({
              actionTaker: item.ActionTaker,
              actionTaken: item.ActionTaken,
              dateTime: item.DateTime,
              remarks: item.Remarks,
            });
          });
          show();
        }
      });
  });
});
