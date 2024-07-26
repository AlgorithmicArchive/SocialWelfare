$(document).ready(function () {
  function element(item, index, randomColor) {
    const actionTaken =
      item.actionTaken == "ReturnToEdit"
        ? "Returned to citizen for editing."
        : item.actionTaken;
    return `<div class="custom-card  flex-column px-2" id="${index}" style="cursor:pointer;border-radius:15px;background-color:${randomColor.bg};color:${randomColor.text}">
            <p class="w-100 text-center fs-6 fw-bold py-2">${item.actionTaker}</p>
            <p class="w-100 text-center fs-6 fw-bold py-2">${actionTaken}</p>
            <p class="w-100 text-center fs-6 fw-bold py-2">${item.dateTime}</p>
           </div>`;
  }

  const shuffleArray = (array) => {
    for (let i = array.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [array[i], array[j]] = [array[j], array[i]];
    }
  };

  function show() {
    const container = $("#displayList");
    container.empty();
    let position = "leftToRight";

    const colorPalette = [
      { bg: "#FF5733", text: "#FFFFFF" }, // Bright Red with White Text
      { bg: "#33FF57", text: "#000000" }, // Bright Green with Black Text
      { bg: "#3357FF", text: "#FFFFFF" }, // Bright Blue with White Text
      { bg: "#FF33A1", text: "#FFFFFF" }, // Bright Pink with White Text
      { bg: "#FFD700", text: "#8B4513" }, // Gold with Saddle Brown Text
      { bg: "#FF6347", text: "#FFFFFF" }, // Tomato with White Text
      { bg: "#32CD32", text: "#000000" }, // Lime Green with Black Text
      { bg: "#1E90FF", text: "#FFFFFF" }, // Dodger Blue with White Text
      { bg: "#FF4500", text: "#FFFFFF" }, // Orange Red with White Text
      { bg: "#ADFF2F", text: "#000000" }, // Green Yellow with Black Text
      { bg: "#00BFFF", text: "#FFFFFF" }, // Deep Sky Blue with White Text
      { bg: "#FF1493", text: "#FFFFFF" }, // Deep Pink with White Text
      { bg: "#FFFF00", text: "#8B4513" }, // Yellow with Saddle Brown Text
      { bg: "#FF8C00", text: "#FFFFFF" }, // Dark Orange with White Text
      { bg: "#3CB371", text: "#000000" }, // Medium Sea Green with Black Text
      { bg: "#4682B4", text: "#FFFFFF" }, // Steel Blue with White Text
      { bg: "#DC143C", text: "#FFFFFF" }, // Crimson with White Text
      { bg: "#7FFF00", text: "#000000" }, // Chartreuse with Black Text
      { bg: "#4169E1", text: "#FFFFFF" }, // Royal Blue with White Text
      { bg: "#C71585", text: "#FFFFFF" }, // Medium Violet Red with White Text
    ];
    for (let i = 0; i < arr.length; i += 3) {
      shuffleArray(colorPalette);
      const row = $(
        '<div class="row d-flex justify-content-between align-items-center"/>'
      );
      const subArray = arr.slice(i, i + 3);

      if (position === "leftToRight") {
        subArray.forEach((item, index) => {
          const randomColor =
            colorPalette[Math.floor(Math.random() * colorPalette.length)];
          row.append(
            `<div class="col-md-4 w-25 p-3">${element(
              item,
              index,
              randomColor
            )}</div>`
          );

          if (index !== subArray.length - 1) {
            row.append(
              '<img class="img-fluid" style="width:5vw" src="/resources/rightArrow.png" />'
            );
          }
        });
        if (subArray.length < 3) {
          row.append(
            `<img class="img-fluid" style="width:3vw;opacity:0" src="/resources/rightArrow.png" /><div class="col-md-4 w-25 p-3"></div>`
          );
        }
        position = "rightToLeft";
      } else {
        subArray.reverse().forEach((item, index) => {
          const randomColor =
            colorPalette[Math.floor(Math.random() * colorPalette.length)];
          const offsetClass =
            subArray.length !== 3 && index === 0
              ? `offset-md-${4 * (3 - subArray.length)}`
              : "";
          if (subArray.length !== 3) {
            row.append(
              '<img class="img-fluid" style="width:3vw;opacity:0" src="/resources/leftArrow.png" />'
            );
          }
          row.append(
            `<div class="col-md-4 ${offsetClass} w-25 p-3">${element(
              item,
              index,
              randomColor
            )}</div>`
          );
          if (subArray.length > 1 && index !== subArray.length - 1) {
            row.append(
              '<img class="img-fluid" style="width:5vw" src="/resources/leftArrow.png" />'
            );
          }
        });
        position = "leftToRight";
      }

      container.append(row);
      if (i + 3 < arr.length) {
        container.append(
          `<div class="row d-flex p-0 mt-0 mb-0 ${
            position === "rightToLeft"
              ? "justify-content-end"
              : "justify-content-start"
          }">
                    <div class="${
                      position === "rightToLeft" ? "offset-md-8" : ""
                    } col-md-4 w-25 p-3 d-flex justify-content-center">
                        <img class="img-fluid" style="width:5vw" src="/resources/downArrow.png" />
                    </div>
                </div>`
        );
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
          $("#errorMsg").text("");
          const history = JSON.parse(data.result.history);
          arr = [];
          history.forEach((item) => {
            arr.push({
              actionTaker: item.ActionTaker,
              actionTaken: item.ActionTaken,
              dateTime: item.DateTime,
              remarks: item.Remarks,
            });
          });
          show();
        } else {
          $("#errorMsg").text(data.response);
        }
      });
  });
});
