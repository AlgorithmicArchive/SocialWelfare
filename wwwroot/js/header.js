$(document).ready(function () {
  const textElements = $("h1, h2, h3, h4, h5, h6, p, span, div,label,button").not("header");

  // Store the default font size of each element when the page loads
  textElements.each(function () {
    $(this).attr("default-size", $(this).css("font-size"));
  });

  function IncreaseFont() {
    textElements.each(function () {
      if ($(this).text().trim().length > 0) {
        let currentSize = parseFloat($(this).css("font-size"));
        $(this).css("font-size", currentSize + 2 + "px");
      }
    });
  }

  function DecreaseFont() {
    textElements.each(function () {
      if ($(this).text().trim().length > 0) {
        let currentSize = parseFloat($(this).css("font-size"));
        $(this).css("font-size", currentSize - 2 + "px");
      }
    });
  }

  function ResetFont() {
    textElements.each(function () {
      // Reset to the stored default font size
      $(this).css("font-size", $(this).attr("default-size"));
    });
  }

  // Handle the font size change event
  $("#fontSize").on("change", function () {
    const value = $(this).val();
    if (value == "+") {
      IncreaseFont();
    } else if (value == "-") {
      DecreaseFont();
    } else if (value == "default") {
      ResetFont();
    }
    $(this).val("");
  });
});
