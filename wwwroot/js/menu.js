$(document).ready(function () {
  const menus = {
    home: {
      left: [{ label: "Home", link: "/" }],
      right: [{ label: "Login/Register", link: "/Home/Authentication" }],
      profile: [],
    },
    user: {
      left: [
        { label: "Home", link: "/User/Index" },
        { label: "Apply for Services", link: "/User/ServicesList" },
      ],
      right: [
        {
          label: "View Status of Application",
          dropdown: [
            {
              label: "Track Application Status",
              link: "/User/ApplicationStatus",
            },
            {
              label: "View Incomplete Applications",
              link: "/User/IncompleteApplications",
            },
          ],
        },
        { label: "Submit Feedback", link: "/User/Feedback" },
      ],
      profile: [
        {
          label: userName,
          dropdown: [
            { label: "Manage Profile", link: "/Profile/Index" },
            { label: "Settings", link: "/Profile/Settings" },
            { label: "Logout", link: "/Home/Logout" },
          ],
        },
      ],
    },
    officer: {
      left: [
        { label: "Home", link: "/Officer/Index" },
        {
          label: "Message Inbox",
          dropdown: [
            {
              label: "Recieved Applications",
              link: "/Officer/Applications?type=Pending",
            },
            {
              label: "Sent Applications",
              link: "/Officer/Applications?type=Sent",
            },
            { label: "Update Requests", link: "/Officer/UpdateRequests" },
          ],
        },
      ],
      right: [
        { label: "DSC Management", link: "/Officer/DSCManagement" },
        { label: "Reports", link: "/Officer/Reports" },
      ],
      profile: [
        {
          label: userName,
          dropdown: [
            { label: "Manage Profile", link: "/Profile/Index" },
            { label: "Logout", link: "/Home/Logout" },
          ],
        },
      ],
    },
  };

  let Type =
    userType == "" ? "home" : userType == "Citizen" ? "user" : "officer";

  const { left: LeftMenu, right: RightMenu, profile: Profile } = menus[Type];

  const $leftContainer = $("#leftContainer");
  const $rightContainer = $("#rightContainer");
  const $profileContainer = $("#profileContainer");

  function renderMenuItems(menuItems, container) {
    const id = container.attr("id");
    menuItems.forEach((item) => {
      let element;
      if (item.dropdown) {
        const dropdownItems = item.dropdown
          .map(
            (dropItem) =>
              `<li><a class="dropdown-item" href="${dropItem.link}">${dropItem.label}</a></li>`
          )
          .join("");
        element = `
          <li class="nav-item ${
            id == "profileContainer" ? "dropstart " : "dropend "
          }">
            <a class="nav-link dropdown-toggle" href="#" id="navbarDropdownMenuLink" role="button"
                data-bs-toggle="dropdown" aria-expanded="false">
                ${item.label}
            </a>
            <ul class="dropdown-menu p-3 border-0 rounded shadow" aria-labelledby="navbarDropdownMenuLink">
                ${dropdownItems}
            </ul>
          </li>`;
      } else {
        element = `
          <li class="nav-item">
            <a class="nav-link" href="${item.link}">${item.label}</a>
          </li>`;
      }
      container.append(element);
    });
  }

  renderMenuItems(LeftMenu, $leftContainer);
  renderMenuItems(RightMenu, $rightContainer);
  renderMenuItems(Profile, $profileContainer);
});
