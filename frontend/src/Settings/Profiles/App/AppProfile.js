import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Card from 'Components/Card';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import EditAppProfileModalConnector from './EditAppProfileModalConnector';
import styles from './AppProfile.css';

class AppProfile extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditAppProfileModalOpen: false,
      isDeleteAppProfileModalOpen: false
    };
  }

  //
  // Listeners

  onEditAppProfilePress = () => {
    this.setState({ isEditAppProfileModalOpen: true });
  };

  onEditAppProfileModalClose = () => {
    this.setState({ isEditAppProfileModalOpen: false });
  };

  onDeleteAppProfilePress = () => {
    this.setState({
      isEditAppProfileModalOpen: false,
      isDeleteAppProfileModalOpen: true
    });
  };

  onDeleteAppProfileModalClose = () => {
    this.setState({ isDeleteAppProfileModalOpen: false });
  };

  onConfirmDeleteAppProfile = () => {
    this.props.onConfirmDeleteAppProfile(this.props.id);
  };

  onCloneAppProfilePress = () => {
    const {
      id,
      onCloneAppProfilePress
    } = this.props;

    onCloneAppProfilePress(id);
  };

  //
  // Render

  render() {
    const {
      id,
      name,
      enableRss,
      enableAutomaticSearch,
      enableInteractiveSearch,
      isDeleting
    } = this.props;

    return (
      <Card
        className={styles.appProfile}
        overlayContent={true}
        onPress={this.onEditAppProfilePress}
      >
        <div className={styles.nameContainer}>
          <div className={styles.name}>
            {name}
          </div>

          <IconButton
            className={styles.cloneButton}
            title={translate('CloneProfile')}
            name={icons.CLONE}
            onPress={this.onCloneAppProfilePress}
          />
        </div>

        <div className={styles.enabled}>
          {
            <Label
              kind={enableRss ? kinds.SUCCESS : kinds.DISABLED}
              outline={!enableRss}
            >
              {translate('Rss')}
            </Label>
          }

          {
            <Label
              kind={enableAutomaticSearch ? kinds.SUCCESS : kinds.DISABLED}
              outline={!enableAutomaticSearch}
            >
              {translate('AutomaticSearch')}
            </Label>
          }

          {
            <Label
              kind={enableInteractiveSearch ? kinds.SUCCESS : kinds.DISABLED}
              outline={!enableInteractiveSearch}
            >
              {translate('InteractiveSearch')}
            </Label>
          }
        </div>

        <EditAppProfileModalConnector
          id={id}
          isOpen={this.state.isEditAppProfileModalOpen}
          onModalClose={this.onEditAppProfileModalClose}
          onDeleteAppProfilePress={this.onDeleteAppProfilePress}
        />

        <ConfirmModal
          isOpen={this.state.isDeleteAppProfileModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteAppProfile')}
          message={translate('DeleteAppProfileMessageText', { name })}
          confirmLabel={translate('Delete')}
          isSpinning={isDeleting}
          onConfirm={this.onConfirmDeleteAppProfile}
          onCancel={this.onDeleteAppProfileModalClose}
        />
      </Card>
    );
  }
}

AppProfile.propTypes = {
  id: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  enableRss: PropTypes.bool.isRequired,
  enableAutomaticSearch: PropTypes.bool.isRequired,
  enableInteractiveSearch: PropTypes.bool.isRequired,
  isDeleting: PropTypes.bool.isRequired,
  onConfirmDeleteAppProfile: PropTypes.func.isRequired,
  onCloneAppProfilePress: PropTypes.func.isRequired
};

export default AppProfile;
