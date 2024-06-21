$(document).ready(function () {
  let list = [];
  let start = 0;
  let last = 3;
  let size = list.length;
  let visible = [];

  function reVisible() {
    $("#displayList").empty();
    visible.forEach((item, index) => {
      $("#displayList").append(`
        <div class="custom-card level-card bg-primary flex-column" id="${index}" style="cursor:pointer">
            <p class="text-white w-100 text-center fs-6 fw-bold">${
              item.actionTaker
            }</p>
            <p class="text-white w-100 text-center fs-6 fw-bold">${
              item.actionTaken
            }</p>
        </div>
        ${
          index !== visible.length - 1
            ? '<i class="fa-solid fa-arrow-right"></i>'
            : ""
        }
    `);
    });
  }
  $("#next").click(function () {
    if (last < size) {
      start++;
      last++;
      visible = list.slice(start, last);
      reVisible();
    }
  });

  $("#previous").click(function () {
    if (start > 0) {
      start--;
      last--;
      visible = list.slice(start, last);
      reVisible();
    }
  });

  $("#getFlow").click(function () {
    const applicationId = $("#applicationId").val();
    console.log(applicationId);
    fetch("/Admin/GetHistory?applicationId=" + applicationId)
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          const history = JSON.parse(data.result.history);
          history.forEach((item) => {
            list.push({
              actionTaker: item.ActionTaker,
              actionTaken: item.ActionTaken,
              dateTime: item.DateTime,
              remarks: item.Remarks,
            });
          });
          console.log(list);
          $("#displayList").show();
          $("#buttons").show();
          visible = list.slice(start, last);
          size = list.length;
          reVisible();
        }
      });
  });

  $(document).on("click", ".level-card", function () {
    const id = $(this).attr("id");
    const item = visible[id];
    $("#displayDetails").show();
    $("#displayDetails").empty();
    $("#displayDetails").append(`
        <div class="w-100 justify-content-start"><label class="fs-3" for="Action Taker">Action Taken By</label>: <span id="ActionTaker"></span></div>
        <div class="w-100 justify-content-start"><label class="fs-3" for="Action Taken">Action Taken</label>: <span id="ActionTaken"></span></div>
        <div class="w-100 justify-content-start"><label class="fs-3" for="Action Taken On">Action Taken On</label>: <span id="DateTime"></span></div>
        <div class="w-100 justify-content-start"><label class="fs-3" for="Remarks">Remarks</label>: <span id="remarks"></span></div>
    `);
    $("#ActionTaker").text(item.actionTaker);
    $("#ActionTaken").text(item.actionTaken);
    $("#DateTime").text(item.dateTime);
    $("#remarks").text(item.remarks);
  });
});
